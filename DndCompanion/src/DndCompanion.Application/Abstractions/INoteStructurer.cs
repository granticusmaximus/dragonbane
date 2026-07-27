using DndCompanion.Domain;

namespace DndCompanion.Application.Abstractions;

public sealed record NoteDraft(NoteKind Kind, string? Subject, string Text, double TStart);

/// <summary>
/// Turns a rolling transcript window into draft notes via a local LLM (Ollama).
/// Output is always a DRAFT — the UI requires user confirmation before persisting.
/// </summary>
public interface INoteStructurer
{
    Task<IReadOnlyList<NoteDraft>> StructureAsync(
        string transcriptWindow, CancellationToken ct = default);
}
