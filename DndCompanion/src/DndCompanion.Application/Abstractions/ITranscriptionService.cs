namespace DndCompanion.Application.Abstractions;

public sealed record TranscriptChunk(double TStart, double TEnd, string Text, double? Confidence);

/// <summary>Local speech-to-text (implemented with Whisper.net in Infrastructure).</summary>
public interface ITranscriptionService
{
    /// <summary>Transcribes a batch of mono 16 kHz float samples (-1.0..1.0). Takes raw
    /// samples rather than a Stream — Whisper.net accepts them directly, so there's no
    /// reason to pay for a WAV-encode/decode round-trip just to satisfy a Stream-shaped
    /// abstraction.</summary>
    IAsyncEnumerable<TranscriptChunk> TranscribeAsync(
        ReadOnlyMemory<float> pcmSamples, CancellationToken ct = default);
}
