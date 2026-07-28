namespace DndCompanion.Application.Abstractions;

/// <summary>Captures microphone audio as mono 16 kHz float samples (implemented with
/// PortAudioSharp2 in Infrastructure — NAudio doesn't support macOS capture).</summary>
public interface IAudioRecorder
{
    int SampleRate { get; }
    bool IsRecording { get; }

    /// <summary>Raised off the audio driver's callback thread — subscribers must marshal
    /// back to their own synchronization context before touching UI state.</summary>
    event Action<float[]>? SamplesCaptured;

    void Start();
    void Stop();
}
