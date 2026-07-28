using System.Text;
using System.Text.Json;
using DndCompanion.Application.Abstractions;
using DndCompanion.Application.Notes;

namespace DndCompanion.Infrastructure.Notes;

/// <summary>
/// Turns a transcript window into draft <see cref="NoteDraft"/>s via a local Ollama model.
/// Assumes an already-running local Ollama server (http://localhost:11434 by default) — if
/// it's unreachable or returns something unparseable, StructureAsync degrades to an empty
/// list rather than throwing; note structuring is assistive, never required for a recording
/// to work.
/// </summary>
public sealed class OllamaNoteStructurer : INoteStructurer, IDisposable
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaNoteStructurer(string model = "llama3.2", string baseUrl = "http://localhost:11434")
    {
        _model = model;
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<IReadOnlyList<NoteDraft>> StructureAsync(string transcriptWindow, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(transcriptWindow)) return [];

        var requestBody = JsonSerializer.Serialize(new
        {
            model = _model,
            stream = false,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = transcriptWindow }
            },
            format = ResponseSchema
        });

        string responseBody;
        try
        {
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync("/api/chat", content, ct);
            response.EnsureSuccessStatusCode();
            responseBody = await response.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException)
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var messageContent = doc.RootElement.GetProperty("message").GetProperty("content").GetString();
            return string.IsNullOrWhiteSpace(messageContent) ? [] : NoteDraftJsonParser.Parse(messageContent);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void Dispose() => _http.Dispose();

    private const string SystemPrompt = """
        You convert an excerpt of a D&D session transcript into short structured notes for
        the players' campaign log. Each transcript line is prefixed with its timestamp in
        seconds, like "[123.4] some spoken text".

        Only emit a note for something noteworthy: a character's turn or action, a move to a
        new location, loot found or distributed, or a key story beat. Skip small talk, rules
        chatter, and filler. If nothing noteworthy is in this excerpt, return an empty list.

        Keep each note's text to one short sentence, past tense, third person. "subject" is
        usually a character or place name — use null if none applies. "tStart" must be the
        timestamp of the transcript line the note is drawn from.
        """;

    // Ollama structured-output schema (response is forced to match this shape).
    private static readonly object ResponseSchema = new
    {
        type = "object",
        properties = new
        {
            notes = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        kind = new { type = "string", @enum = new[] { "Turn", "Move", "Action", "Location", "Loot", "Other" } },
                        subject = new { type = new[] { "string", "null" } },
                        text = new { type = "string" },
                        tStart = new { type = "number" }
                    },
                    required = new[] { "kind", "text", "tStart" }
                }
            }
        },
        required = new[] { "notes" }
    };
}
