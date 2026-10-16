using System.Net;
using System.Security.Cryptography;
using DndCompanion.Application.Abstractions;
using DndCompanion.Infrastructure;
using DndCompanion.Infrastructure.Models;

namespace DndCompanion.Tests;

public class LocalModelStoreTests : IDisposable
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "dndcompanion-models-" + Guid.NewGuid());
    private readonly byte[] _payload = RandomNumberGenerator.GetBytes(300_000);

    public void Dispose()
    {
        if (Directory.Exists(_dataDir)) Directory.Delete(_dataDir, recursive: true);
    }

    private LocalModelStore CreateStore(FakeModelServer server, string? sha256 = null) =>
        new(new AppPaths(_dataDir),
            new Dictionary<LocalModel, ModelSpec>
            {
                [LocalModel.NoteDrafting] = new("Test model", "model.gguf", new Uri("https://models.test/model.gguf"),
                    _payload.Length, sha256 ?? Convert.ToHexStringLower(SHA256.HashData(_payload)))
            },
            server);

    private string ModelsDir => Path.Combine(_dataDir, "models");

    [Fact]
    public async Task Downloads_verifies_and_installs_model()
    {
        using var store = CreateStore(new FakeModelServer(_payload));
        Assert.Equal(ModelInstallState.NotInstalled, store.GetStatus(LocalModel.NoteDrafting).State);

        await store.DownloadAsync(LocalModel.NoteDrafting);

        var status = store.GetStatus(LocalModel.NoteDrafting);
        Assert.Equal(ModelInstallState.Installed, status.State);
        Assert.Equal(_payload, await File.ReadAllBytesAsync(store.GetPath(LocalModel.NoteDrafting)));
        Assert.False(File.Exists(store.GetPath(LocalModel.NoteDrafting) + ".partial"));
    }

    [Fact]
    public async Task Checksum_mismatch_fails_and_leaves_nothing_installed()
    {
        using var store = CreateStore(new FakeModelServer(_payload), sha256: new string('0', 64));

        await store.DownloadAsync(LocalModel.NoteDrafting);

        var status = store.GetStatus(LocalModel.NoteDrafting);
        Assert.Equal(ModelInstallState.Failed, status.State);
        Assert.Contains("checksum", status.Error);
        Assert.Empty(Directory.GetFiles(ModelsDir));
    }

    [Fact]
    public async Task Resumes_from_partial_file_with_a_range_request()
    {
        var server = new FakeModelServer(_payload);
        using var store = CreateStore(server);
        Directory.CreateDirectory(ModelsDir);
        await File.WriteAllBytesAsync(store.GetPath(LocalModel.NoteDrafting) + ".partial", _payload[..100_000]);

        await store.DownloadAsync(LocalModel.NoteDrafting);

        Assert.Equal(100_000, server.LastRangeFrom);
        Assert.Equal(ModelInstallState.Installed, store.GetStatus(LocalModel.NoteDrafting).State);
        Assert.Equal(_payload, await File.ReadAllBytesAsync(store.GetPath(LocalModel.NoteDrafting)));
    }

    [Fact]
    public async Task Starts_over_when_server_ignores_the_range_request()
    {
        var server = new FakeModelServer(_payload) { SupportsRanges = false };
        using var store = CreateStore(server);
        Directory.CreateDirectory(ModelsDir);
        await File.WriteAllBytesAsync(store.GetPath(LocalModel.NoteDrafting) + ".partial", _payload[..100_000]);

        await store.DownloadAsync(LocalModel.NoteDrafting);

        Assert.Equal(ModelInstallState.Installed, store.GetStatus(LocalModel.NoteDrafting).State);
        Assert.Equal(_payload, await File.ReadAllBytesAsync(store.GetPath(LocalModel.NoteDrafting)));
    }

    [Fact]
    public async Task Complete_but_unverified_partial_is_installed_without_a_request()
    {
        var server = new FakeModelServer(_payload);
        using var store = CreateStore(server);
        Directory.CreateDirectory(ModelsDir);
        await File.WriteAllBytesAsync(store.GetPath(LocalModel.NoteDrafting) + ".partial", _payload);

        await store.DownloadAsync(LocalModel.NoteDrafting);

        Assert.Equal(0, server.RequestCount);
        Assert.Equal(ModelInstallState.Installed, store.GetStatus(LocalModel.NoteDrafting).State);
    }

    [Fact]
    public async Task Concurrent_callers_share_one_download()
    {
        var server = new FakeModelServer(_payload);
        using var store = CreateStore(server);

        await Task.WhenAll(store.DownloadAsync(LocalModel.NoteDrafting), store.DownloadAsync(LocalModel.NoteDrafting));

        Assert.Equal(1, server.RequestCount);
    }

    [Fact]
    public async Task Http_error_is_reported_as_failed_not_thrown()
    {
        using var store = CreateStore(new FakeModelServer(_payload) { FailWith = HttpStatusCode.ServiceUnavailable });

        await store.DownloadAsync(LocalModel.NoteDrafting);

        Assert.Equal(ModelInstallState.Failed, store.GetStatus(LocalModel.NoteDrafting).State);
    }

    [Fact]
    public void Already_installed_model_reports_installed_without_downloading()
    {
        var server = new FakeModelServer(_payload);
        using var store = CreateStore(server);
        Directory.CreateDirectory(ModelsDir);
        File.WriteAllBytes(store.GetPath(LocalModel.NoteDrafting), _payload);

        Assert.True(store.DownloadAsync(LocalModel.NoteDrafting).IsCompletedSuccessfully);
        Assert.Equal(ModelInstallState.Installed, store.GetStatus(LocalModel.NoteDrafting).State);
        Assert.Equal(0, server.RequestCount);
    }

    private sealed class FakeModelServer(byte[] payload) : HttpMessageHandler
    {
        public bool SupportsRanges { get; init; } = true;
        public HttpStatusCode? FailWith { get; init; }
        public int RequestCount { get; private set; }
        public long? LastRangeFrom { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestCount++;
            if (FailWith is { } code) return Task.FromResult(new HttpResponseMessage(code));

            var from = request.Headers.Range?.Ranges.First().From;
            LastRangeFrom = from;
            if (from is > 0 && SupportsRanges)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.PartialContent)
                {
                    Content = new ByteArrayContent(payload[(int)from.Value..])
                });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
        }
    }
}
