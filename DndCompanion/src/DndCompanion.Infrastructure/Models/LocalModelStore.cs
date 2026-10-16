using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using DndCompanion.Application.Abstractions;

namespace DndCompanion.Infrastructure.Models;

/// <summary>
/// Where to fetch one model file and how to verify it. URLs are pinned to a Hugging Face
/// commit, so an upstream re-upload can't silently swap the file out from under the
/// checksum.
/// </summary>
public sealed record ModelSpec(string DisplayName, string FileName, Uri DownloadUrl, long SizeBytes, string Sha256);

/// <summary>
/// Singleton — downloads run on the store's own lifetime, not a Blazor circuit's, so they
/// survive navigation. "Installed" means the file exists at its expected size; the SHA-256 is
/// checked once at download time rather than re-hashing gigabytes on every launch.
/// Downloads go to "{file}.partial" and resume from it via an HTTP Range request.
/// </summary>
public sealed class LocalModelStore : ILocalModelStore, IDisposable
{
    public static readonly IReadOnlyDictionary<LocalModel, ModelSpec> DefaultCatalog = new Dictionary<LocalModel, ModelSpec>
    {
        // Same file WhisperGgmlDownloader fetched before this store existed, so existing
        // installs are recognized as already downloaded. MIT licensed.
        [LocalModel.Transcription] = new(
            "Whisper base — speech-to-text",
            "ggml-base.bin",
            new Uri("https://huggingface.co/ggerganov/whisper.cpp/resolve/5359861c739e955e79d9a303bcbc70fb988958b1/ggml-base.bin"),
            147_951_465,
            "60ed5bc3dd14eea856493d334349b405782ddcaf0028d4b5df4088345fba2efe"),
        // Llama 3.2 3B Instruct, Q4_K_M — the same model/quantization Ollama's `llama3.2`
        // tag serves. Llama 3.2 Community License.
        [LocalModel.NoteDrafting] = new(
            "Llama 3.2 3B — note drafting",
            "Llama-3.2-3B-Instruct-Q4_K_M.gguf",
            new Uri("https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/5ab33fa94d1d04e903623ae72c95d1696f09f9e8/Llama-3.2-3B-Instruct-Q4_K_M.gguf"),
            2_019_377_696,
            "6c1a2b41161032677be168d354123594c0e6e67d2b9227c84f296ad037c728ff"),
    };

    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(250);

    private readonly string _modelsDir;
    private readonly IReadOnlyDictionary<LocalModel, ModelSpec> _catalog;
    private readonly HttpClient _http;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Lock _lock = new();
    private readonly Dictionary<LocalModel, Task> _inFlight = [];
    private readonly Dictionary<LocalModel, (long Bytes, string? Error)> _progress = [];

    public LocalModelStore(
        IAppPaths paths,
        IReadOnlyDictionary<LocalModel, ModelSpec>? catalog = null,
        HttpMessageHandler? handler = null)
    {
        _modelsDir = Path.Combine(paths.DataDirectory, "models");
        _catalog = catalog ?? DefaultCatalog;
        // Multi-GB downloads: no overall timeout; stalls surface as IOExceptions instead.
        _http = new HttpClient(handler ?? new SocketsHttpHandler()) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public event Action? StatusChanged;

    public string GetPath(LocalModel model) => Path.Combine(_modelsDir, _catalog[model].FileName);

    public IReadOnlyList<LocalModelStatus> GetAll() => _catalog.Keys.Select(GetStatus).ToList();

    public LocalModelStatus GetStatus(LocalModel model)
    {
        var spec = _catalog[model];
        lock (_lock)
        {
            _progress.TryGetValue(model, out var p);
            if (_inFlight.ContainsKey(model))
                return new(model, spec.DisplayName, spec.SizeBytes, ModelInstallState.Downloading, p.Bytes, null);
            if (IsInstalled(model))
                return new(model, spec.DisplayName, spec.SizeBytes, ModelInstallState.Installed, spec.SizeBytes, null);
            if (p.Error is not null)
                return new(model, spec.DisplayName, spec.SizeBytes, ModelInstallState.Failed, p.Bytes, p.Error);
            return new(model, spec.DisplayName, spec.SizeBytes, ModelInstallState.NotInstalled, PartialLength(model), null);
        }
    }

    public Task DownloadAsync(LocalModel model)
    {
        lock (_lock)
        {
            if (_inFlight.TryGetValue(model, out var running)) return running;
            if (IsInstalled(model)) return Task.CompletedTask;

            _progress[model] = (PartialLength(model), null);
            var task = Task.Run(() => RunDownloadAsync(model));
            _inFlight[model] = task;
            return task;
        }
    }

    private bool IsInstalled(LocalModel model)
    {
        var file = new FileInfo(GetPath(model));
        return file.Exists && file.Length == _catalog[model].SizeBytes;
    }

    private string PartialPath(LocalModel model) => GetPath(model) + ".partial";

    private long PartialLength(LocalModel model)
    {
        var partial = new FileInfo(PartialPath(model));
        return partial.Exists ? partial.Length : 0;
    }

    private async Task RunDownloadAsync(LocalModel model)
    {
        string? error = null;
        try
        {
            await DownloadToFileAsync(model, _shutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            // App shutting down — keep the .partial so the next launch resumes.
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException)
        {
            error = ex.Message;
        }
        finally
        {
            lock (_lock)
            {
                _inFlight.Remove(model);
                _progress[model] = (PartialLength(model), error);
            }
            RaiseStatusChanged();
        }
    }

    private async Task DownloadToFileAsync(LocalModel model, CancellationToken ct)
    {
        var spec = _catalog[model];
        var finalPath = GetPath(model);
        var partialPath = PartialPath(model);
        Directory.CreateDirectory(_modelsDir);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long existing = PartialLength(model);
        if (existing > spec.SizeBytes)
        {
            File.Delete(partialPath);
            existing = 0;
        }
        else if (existing == spec.SizeBytes)
        {
            // Fully downloaded but never verified/renamed (e.g. the app quit in between) —
            // a Range request past the end would just 416, so verify what's on disk.
            await using (var complete = File.OpenRead(partialPath))
                await HashStreamAsync(complete, hash, ct);
            VerifyAndInstall(hash, spec, partialPath, finalPath);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, spec.DownloadUrl);
        if (existing > 0) request.Headers.Range = new RangeHeaderValue(existing, null);

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        // Server ignored the Range header (200, not 206): start over rather than append a
        // second full copy onto the partial file.
        var resuming = existing > 0 && response.StatusCode == HttpStatusCode.PartialContent;
        if (resuming)
        {
            await using var prior = File.OpenRead(partialPath);
            await HashStreamAsync(prior, hash, ct);
        }
        else
        {
            existing = 0;
        }

        long downloaded = existing;
        await using (var output = new FileStream(partialPath, resuming ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
        await using (var input = await response.Content.ReadAsStreamAsync(ct))
        {
            var buffer = new byte[1 << 16];
            var sinceReport = Stopwatch.StartNew();
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
                hash.AppendData(buffer, 0, read);
                downloaded += read;

                if (sinceReport.Elapsed >= ProgressInterval)
                {
                    lock (_lock) _progress[model] = (downloaded, null);
                    RaiseStatusChanged();
                    sinceReport.Restart();
                }
            }
        }

        if (downloaded != spec.SizeBytes)
            throw new IOException($"Download ended early ({downloaded:N0} of {spec.SizeBytes:N0} bytes). Try again to resume.");

        VerifyAndInstall(hash, spec, partialPath, finalPath);
    }

    private static void VerifyAndInstall(IncrementalHash hash, ModelSpec spec, string partialPath, string finalPath)
    {
        var actual = Convert.ToHexStringLower(hash.GetHashAndReset());
        if (!string.Equals(actual, spec.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(partialPath);
            throw new InvalidDataException("Downloaded file failed its checksum and was deleted. Try again.");
        }

        File.Move(partialPath, finalPath, overwrite: true);
    }

    private static async Task HashStreamAsync(Stream stream, IncrementalHash hash, CancellationToken ct)
    {
        var buffer = new byte[1 << 16];
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            hash.AppendData(buffer, 0, read);
    }

    private void RaiseStatusChanged() => StatusChanged?.Invoke();

    public void Dispose()
    {
        _shutdown.Cancel();
        _http.Dispose();
        _shutdown.Dispose();
    }
}
