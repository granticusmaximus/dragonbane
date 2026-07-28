using System.Text.Json;
using System.Text.Json.Serialization;
using DndCompanion.Application.Abstractions;
using DndCompanion.Domain;

namespace DndCompanion.Application.Notes;

/// <summary>
/// Parses the JSON an LLM (Ollama) returns for note structuring into <see cref="NoteDraft"/>s.
/// Pure and side-effect free so it's testable without a live model — the HTTP call lives in
/// OllamaNoteStructurer, which only needs to hand this the response body string.
/// Never throws: malformed output degrades to zero drafts rather than breaking a recording
/// session over a bad LLM response — structuring is assistive, not required.
/// </summary>
public static class NoteDraftJsonParser
{
    public static IReadOnlyList<NoteDraft> Parse(string json)
    {
        ResponseDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ResponseDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        if (payload?.Notes is null) return [];

        var drafts = new List<NoteDraft>();
        foreach (var n in payload.Notes)
        {
            if (n is null || string.IsNullOrWhiteSpace(n.Text)) continue;
            var kind = Enum.TryParse<NoteKind>(n.Kind, ignoreCase: true, out var parsed) ? parsed : NoteKind.Other;
            drafts.Add(new NoteDraft(kind, n.Subject, n.Text.Trim(), n.TStart));
        }
        return drafts;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record ResponseDto([property: JsonPropertyName("notes")] List<NoteEntryDto>? Notes);

    private sealed record NoteEntryDto(
        [property: JsonPropertyName("kind")] string? Kind,
        [property: JsonPropertyName("subject")] string? Subject,
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("tStart")] double TStart);
}
