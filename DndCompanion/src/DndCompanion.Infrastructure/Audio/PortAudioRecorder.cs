using System.Runtime.InteropServices;
using DndCompanion.Application.Abstractions;
using PortAudioSharp;
using PaStream = PortAudioSharp.Stream;

namespace DndCompanion.Infrastructure.Audio;

/// <summary>
/// Mic capture via PortAudioSharp2 — NAudio does not support macOS recording (confirmed:
/// neither the stable 2.x line nor the 3.0 preview's new Linux/ALSA support extend to
/// macOS), so this app uses PortAudio instead, which bundles real CoreAudio-backed
/// native binaries for osx-arm64/osx-x64.
///
/// Lifecycle note verified by hand against this library's actual behavior: calling
/// <see cref="PaStream.Stop"/> followed by <see cref="PaStream.Dispose"/> (or a `using`
/// block) throws a PortAudioException from inside Dispose's Close() call, even though
/// the stream is left in a valid stopped state. Calling Stop() then Close() directly
/// (skipping Dispose entirely) does not throw. This class deliberately never calls
/// Dispose()/using on the underlying Stream.
/// </summary>
public sealed class PortAudioRecorder : IAudioRecorder, IDisposable
{
    public int SampleRate { get; } = 16_000;
    public bool IsRecording { get; private set; }

    public event Action<float[]>? SamplesCaptured;

    private readonly object _lock = new();
    private PaStream? _stream;
    private bool _initialized;

    public void Start()
    {
        lock (_lock)
        {
            if (IsRecording) return;

            if (!_initialized)
            {
                PortAudio.Initialize();
                _initialized = true;
            }

            var deviceIndex = PortAudio.DefaultInputDevice;
            var deviceInfo = PortAudio.GetDeviceInfo(deviceIndex);

            var streamParams = new StreamParameters
            {
                device = deviceIndex,
                channelCount = 1,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = deviceInfo.defaultLowInputLatency,
                hostApiSpecificStreamInfo = IntPtr.Zero
            };

            _stream = new PaStream(streamParams, null, SampleRate, 0, StreamFlags.ClipOff, Callback, IntPtr.Zero);
            _stream.Start();
            IsRecording = true;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!IsRecording || _stream is null) return;

            _stream.Stop();
            _stream.Close(); // not Dispose() -- see class remarks
            _stream = null;
            IsRecording = false;
        }
    }

    private StreamCallbackResult Callback(
        IntPtr input, IntPtr output, uint frameCount,
        ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
    {
        var samples = new float[frameCount];
        Marshal.Copy(input, samples, 0, (int)frameCount);
        SamplesCaptured?.Invoke(samples);
        return StreamCallbackResult.Continue;
    }

    public void Dispose()
    {
        Stop();
        if (_initialized)
        {
            PortAudio.Terminate();
            _initialized = false;
        }
    }
}
