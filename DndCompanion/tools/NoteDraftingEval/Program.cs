using DndCompanion.Application.Abstractions;
using DndCompanion.Infrastructure.Notes;
using Microsoft.Extensions.Logging.Abstractions;

// Runs LlamaNoteStructurer — the real prompt, grammar and sampling settings — over a fixed
// set of transcript excerpts and checks how many notes each one produces. Not part of
// `dotnet test` because it needs the ~2 GB model file. Re-run it before changing the
// prompt, grammar, sampling or model:
//
//   dotnet run --project tools/NoteDraftingEval -- "<path to Llama-3.2-3B-Instruct-Q4_K_M.gguf>"
//
// (After a download the model lives in {data dir}/models/ — on macOS,
// ~/Library/Application Support/DndCompanion/models/.)
// Baseline with the current prompt: 5/6, the miss being "mixed" (finds 1 of 2 events).
// Any note on an expected-0 case is a fabrication — treat that as a regression.

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("usage: NoteDraftingEval <path to .gguf model>");
    return 2;
}

var cases = new (string Name, int Expected, string Excerpt)[]
{
    ("combat+loot+move", 3, "[0.0] Okay, Thorin, it's your turn. I swing my warhammer at the Goblin Chief and hit for 11 damage. The Goblin Chief falls, searching the body, you find 40 gold pieces and a silver key, the party then heads down the stairs into the flooded crypt."),
    ("small talk", 0, "[0.0] So, uh, does anyone want pizza?\n[3.2] Yeah, pepperoni please."),
    ("rules chatter", 0, "[0.0] Wait, does advantage stack?\n[4.1] No, it doesn't stack, you just roll two d20s and take the higher.\n[9.5] Okay cool, thanks."),
    ("scheduling", 0, "[0.0] Are we still on for next Friday?\n[3.0] I might be late, I have work until seven.\n[6.5] No worries, we'll start without you."),
    ("multi-line story", 2, "[0.0] The old innkeeper leans in and whispers that the mayor has been replaced by a doppelganger.\n[8.0] Sera, what do you do?\n[10.0] I grab my bow and follow the innkeeper into the cellar."),
    ("mixed", 2, "[0.0] Can someone pass the chips?\n[5.0] Mira casts fireball at the bandits and three of them go down.\n[9.0] Nice!\n[12.0] Okay everyone, you arrive at the gates of Neverwinter."),
};

using var structurer = new LlamaNoteStructurer(new FixedModelStore(args[0]), NullLogger<LlamaNoteStructurer>.Instance);
int passed = 0, fabrications = 0;
foreach (var c in cases)
{
    var notes = await structurer.StructureAsync(c.Excerpt);
    var ok = notes.Count == c.Expected;
    if (ok) passed++;
    if (c.Expected == 0) fabrications += notes.Count;
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {c.Name}: expected {c.Expected}, got {notes.Count}");
    foreach (var n in notes) Console.WriteLine($"     {n.Kind} | {n.Subject ?? "null"} | {n.Text} | t={n.TStart}");
}
Console.WriteLine($"\n{passed}/{cases.Length} passed, {fabrications} note(s) on excerpts with nothing to note");
return 0;

/// <summary>Points the structurer at an arbitrary model file, already "installed".</summary>
sealed class FixedModelStore(string path) : ILocalModelStore
{
    public string GetPath(LocalModel model) => path;
    public LocalModelStatus GetStatus(LocalModel model) => new(model, Path.GetFileName(path), new FileInfo(path).Length, ModelInstallState.Installed, 0, null);
    public IReadOnlyList<LocalModelStatus> GetAll() => [GetStatus(LocalModel.NoteDrafting)];
    public Task DownloadAsync(LocalModel model) => Task.CompletedTask;
    public event Action? StatusChanged { add { } remove { } }
}
