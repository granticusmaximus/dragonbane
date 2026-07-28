using System.Runtime.CompilerServices;
using DndCompanion.Application.Abstractions;
using Whisper.net;
using Whisper.net.Ggml;

namespace DndCompanion.Infrastructure.Audio;

/// <summary>
/// Local speech-to-text via Whisper.net. The GGML model is downloaded on first use
/// (via <see cref="WhisperGgmlDownloader"/>, sourced from Hugging Face) to
/// <paramref name="modelPath"/> and cached there — never committed to the repo, since
/// even the "base" model is well over 100 MB.
/// </summary>
public sealed class WhisperTranscriptionService(string modelPath) : ITranscriptionService, IDisposable
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private WhisperFactory? _factory;

    public async IAsyncEnumerable<TranscriptChunk> TranscribeAsync(
        ReadOnlyMemory<float> pcmSamples, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var factory = await GetFactoryAsync(ct);
        using var processor = factory.CreateBuilder().WithLanguage("auto").Build();

        await foreach (var segment in processor.ProcessAsync(pcmSamples, ct))
        {
            yield return new TranscriptChunk(
                segment.Start.TotalSeconds,
                segment.End.TotalSeconds,
                segment.Text.Trim(),
                segment.Probability);
        }
    }

    private async Task<WhisperFactory> GetFactoryAsync(CancellationToken ct)
    {
        if (_factory is not null) return _factory;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_factory is not null) return _factory;

            if (!File.Exists(modelPath))
            {
                var dir = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base, cancellationToken: ct);
                using var fileWriter = File.OpenWrite(modelPath);
                await modelStream.CopyToAsync(fileWriter, ct);
            }

            _factory = WhisperFactory.FromPath(modelPath);
            return _factory;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public void Dispose()
    {
        _factory?.Dispose();
        _initLock.Dispose();
    }
}
