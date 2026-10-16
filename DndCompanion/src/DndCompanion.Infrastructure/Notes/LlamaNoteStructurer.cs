using System.Text;
using DndCompanion.Application.Abstractions;
using DndCompanion.Application.Notes;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using Microsoft.Extensions.Logging;

namespace DndCompanion.Infrastructure.Notes;

/// <summary>
/// Turns a transcript window into draft <see cref="NoteDraft"/>s with a local Llama 3.2 model
/// run in-process via LLamaSharp (llama.cpp) — no separate Ollama install or server. Output
/// is constrained by a GBNF grammar to the <c>{"notes":[...]}</c> shape
/// <see cref="NoteDraftJsonParser"/> expects.
///
/// Degrades to an empty list rather than throwing when the model isn't downloaded, fails to
/// load, or produces something unparseable — note drafting is assistive, never required for
/// a recording to work. The ~2 GB of weights are loaded on first use and released after
/// <see cref="IdleUnloadAfter"/> without a request, so they don't sit in RAM for the rest of
/// the app's lifetime after one recording.
/// </summary>
public sealed class LlamaNoteStructurer : INoteStructurer, IDisposable
{
    private static readonly TimeSpan IdleUnloadAfter = TimeSpan.FromMinutes(5);

    private readonly ILocalModelStore _modelStore;
    private readonly ILogger<LlamaNoteStructurer> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Timer _idleTimer;
    private LLamaWeights? _weights;
    private ModelParams? _params;
    private NativeLogConfig.LLamaLogCallback? _nativeLog;

    public LlamaNoteStructurer(ILocalModelStore modelStore, ILogger<LlamaNoteStructurer> logger)
    {
        _modelStore = modelStore;
        _logger = logger;
        _idleTimer = new Timer(_ => _ = UnloadAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task<IReadOnlyList<NoteDraft>> StructureAsync(string transcriptWindow, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transcriptWindow)) return [];
        if (_modelStore.GetStatus(LocalModel.NoteDrafting).State != ModelInstallState.Installed) return [];

        // One inference at a time — a single loaded model, and a laptop has no spare RAM
        // for a second context.
        await _lock.WaitAsync(ct);
        try
        {
            _idleTimer.Change(Timeout.Infinite, Timeout.Infinite);
            var weights = await GetWeightsAsync(ct);

            var executor = new StatelessExecutor(weights, _params!)
            {
                ApplyTemplate = true,
                SystemMessage = SystemPrompt
            };
            var inference = new InferenceParams
            {
                MaxTokens = 1024,
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Grammar = new Grammar(NotesGrammar, "root"),
                    // Greedy decoding: extraction, not creative writing. With any sampling
                    // randomness a 3B model's output varied run to run on the same
                    // transcript (3 notes vs 1, and sometimes a fabricated note for pure
                    // small talk); at 0 it's repeatable.
                    Temperature = 0f
                }
            };

            var output = new StringBuilder();
            await foreach (var token in executor.InferAsync(transcriptWindow, inference, ct))
                output.Append(token);

            return NoteDraftJsonParser.Parse(output.ToString());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Native load failures (unsupported CPU, corrupt file, out of memory) land here
            // too — log and degrade rather than take down the recording page.
            _logger.LogWarning(ex, "Local note drafting failed; returning no drafts");
            return [];
        }
        finally
        {
            _idleTimer.Change(IdleUnloadAfter, Timeout.InfiniteTimeSpan);
            _lock.Release();
        }
    }

    private async Task<LLamaWeights> GetWeightsAsync(CancellationToken ct)
    {
        if (_weights is not null) return _weights;

        // llama.cpp logs every tensor/buffer at Info level straight to stdout; keep only
        // warnings and errors, via the app's logger. Must be set before the first native
        // call, and the delegate kept in a field so the GC can't collect it while native
        // code still holds the pointer.
        if (_nativeLog is null)
        {
            _nativeLog = (level, message) =>
            {
                if (level == LLamaLogLevel.Error) _logger.LogError("llama.cpp: {Message}", message.TrimEnd());
                else if (level == LLamaLogLevel.Warning) _logger.LogWarning("llama.cpp: {Message}", message.TrimEnd());
            };
            NativeLogConfig.llama_log_set(_nativeLog);
        }

        _params = new ModelParams(_modelStore.GetPath(LocalModel.NoteDrafting))
        {
            // A ~60s transcript window is a few hundred tokens; 4K leaves ample headroom
            // for the system prompt and the drafted notes without wasting KV-cache RAM.
            ContextSize = 4096,
            // Offload every layer when a GPU backend is present (Metal on Apple Silicon);
            // ignored by the CPU-only backends.
            GpuLayerCount = 999
        };
        _weights = await LLamaWeights.LoadFromFileAsync(_params, ct);
        return _weights;
    }

    private async Task UnloadAsync()
    {
        try
        {
            await _lock.WaitAsync();
        }
        catch (ObjectDisposedException)
        {
            return; // timer fired during app shutdown — Dispose already released the weights
        }

        try
        {
            _weights?.Dispose();
            _weights = null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _idleTimer.Dispose();
        _weights?.Dispose();
        _lock.Dispose();
    }

    // Tuned against a small fixed set of transcript excerpts (in-game events, small talk,
    // rules questions, scheduling, mixed) on two Llama 3.2 3B Q4_K_M builds. The prompt
    // carried over from the Ollama version scored 1–2/6 under this grammar and invented
    // events outright ("the party found 100 gold pieces" from a rules question); this one
    // scored 5/6 on both with no fabrications — its known miss is occasionally dropping one
    // event from an excerpt that mixes table talk and play. The explicit empty-list
    // example is what stopped the fabrication; don't remove it without re-testing.
    private const string SystemPrompt = """
        You turn an excerpt of a D&D game-night transcript into notes for the campaign log.
        Each line starts with its timestamp in seconds, like "[123.4] spoken text".

        Write a note ONLY for in-game events that are actually stated in the excerpt:
        - Action: a character attacks, casts a spell, or does something in the game world
        - Loot: treasure or items found or handed out
        - Location: the party arrives at or moves to a place
        - Turn, Move, Other: other in-game story events

        Never write a note for out-of-game talk: food, jokes, scheduling, or questions about
        the rules. Never invent anything that is not in the excerpt. Most excerpts contain
        only a few events or none at all; an empty list is a correct and common answer.

        Each note: "text" is one short past-tense sentence; "subject" is the character or
        place it is about, or null; "tStart" is the timestamp of the line it came from.

        Example excerpt:
        [2.0] Anyone want more soda?
        [5.5] Does a natural 20 auto-hit on a spell attack?
        Correct output: {"notes":[]}

        Example excerpt:
        [10.0] Kael picks the lock and the door swings open.
        [14.0] Inside you find a healing potion.
        Correct output: {"notes":[{"kind":"Action","subject":"Kael","text":"Kael picked the lock and opened the door.","tStart":10.0},{"kind":"Loot","subject":"Kael","text":"The party found a healing potion.","tStart":14.0}]}

        Respond with JSON only.
        """;

    // GBNF for {"notes":[{kind, subject, text, tStart}]} — the same shape the Ollama
    // JSON-schema version enforced. Whitespace is capped so the grammar can't let the model
    // pad indefinitely instead of finishing.
    private const string NotesGrammar = """
        root    ::= "{" ws "\"notes\"" ws ":" ws "[" ws ( note ( ws "," ws note )* )? ws "]" ws "}"
        note    ::= "{" ws "\"kind\"" ws ":" ws kind ws "," ws "\"subject\"" ws ":" ws ( string | "null" ) ws "," ws "\"text\"" ws ":" ws string ws "," ws "\"tStart\"" ws ":" ws number ws "}"
        kind    ::= "\"Turn\"" | "\"Move\"" | "\"Action\"" | "\"Location\"" | "\"Loot\"" | "\"Other\""
        string  ::= "\"" ( [^"\\\x00-\x1F] | "\\" ( ["\\/bfnrt] | "u" [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] ) )* "\""
        number  ::= "-"? [0-9]+ ( "." [0-9]+ )?
        ws      ::= [ \t\n]? [ \t\n]? [ \t\n]?
        """;
}
