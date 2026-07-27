namespace DndCompanion.Application.Abstractions;

public sealed record TranscriptChunk(double TStart, double TEnd, string Text, double? Confidence);

/// <summary>Local speech-to-text (implemented with Whisper.net in Infrastructure).</summary>
public interface ITranscriptionService
{
    /// <summary>Stream transcript chunks as audio arrives.</summary>
    IAsyncEnumerable<TranscriptChunk> TranscribeAsync(
        Stream pcmAudio, CancellationToken ct = default);
}
