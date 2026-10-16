namespace DndCompanion.Application.Abstractions;

/// <summary>The on-device AI models the app can download and run locally.</summary>
public enum LocalModel
{
    /// <summary>Speech-to-text (Whisper) — required to record.</summary>
    Transcription,
    /// <summary>LLM that drafts session notes from the transcript — optional.</summary>
    NoteDrafting
}

public enum ModelInstallState { NotInstalled, Downloading, Installed, Failed }

public sealed record LocalModelStatus(
    LocalModel Model,
    string DisplayName,
    long SizeBytes,
    ModelInstallState State,
    long BytesDownloaded,
    string? Error);

/// <summary>
/// Owns the model files under {DataDirectory}/models: whether each is installed, and
/// downloading it (with progress, resume and checksum verification) when it isn't.
/// Models are fetched on first use rather than shipped in the installer — the note-drafting
/// model alone is ~2 GB.
/// </summary>
public interface ILocalModelStore
{
    string GetPath(LocalModel model);

    LocalModelStatus GetStatus(LocalModel model);

    IReadOnlyList<LocalModelStatus> GetAll();

    /// <summary>
    /// Starts (or joins an already-running) download. The download belongs to the store, not
    /// the caller — navigating away from the page that started it doesn't cancel it.
    /// Completes immediately if the model is already installed; never throws — failures
    /// surface as <see cref="ModelInstallState.Failed"/> on <see cref="GetStatus"/>.
    /// </summary>
    Task DownloadAsync(LocalModel model);

    /// <summary>Raised (from a background thread) whenever any model's status or progress changes.</summary>
    event Action? StatusChanged;
}
