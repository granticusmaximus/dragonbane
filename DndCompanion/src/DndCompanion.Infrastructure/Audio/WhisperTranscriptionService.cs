using System.Runtime.CompilerServices;
using DndCompanion.Application.Abstractions;
using Whisper.net;

namespace DndCompanion.Infrastructure.Audio;

/// <summary>
/// Local speech-to-text via Whisper.net. The GGML model file is owned by
/// <see cref="ILocalModelStore"/> (downloaded on first use, ~148 MB — never committed to the
/// repo); if it isn't installed yet when transcription is first requested, this waits for
/// the store to download it.
/// </summary>
public sealed class WhisperTranscriptionService(ILocalModelStore modelStore) : ITranscriptionService, IDisposable
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

            await modelStore.DownloadAsync(LocalModel.Transcription).WaitAsync(ct);
            var status = modelStore.GetStatus(LocalModel.Transcription);
            if (status.State != ModelInstallState.Installed)
                throw new InvalidOperationException($"Speech-to-text model isn't installed: {status.Error ?? "download did not complete"}");

            _factory = WhisperFactory.FromPath(modelStore.GetPath(LocalModel.Transcription));
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
