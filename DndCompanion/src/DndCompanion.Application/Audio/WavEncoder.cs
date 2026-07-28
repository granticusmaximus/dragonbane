namespace DndCompanion.Application.Audio;

/// <summary>Wraps raw mono float samples (-1.0..1.0) into a 16-bit PCM WAV stream —
/// the format Whisper.net's processor expects. Pure/deterministic, no I/O.</summary>
public static class WavEncoder
{
    public static byte[] EncodeMono16Bit(IReadOnlyList<float> samples, int sampleRate)
    {
        const int bitsPerSample = 16;
        const int channels = 1;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = samples.Count * blockAlign;

        using var stream = new MemoryStream(44 + dataSize);
        using var w = new BinaryWriter(stream);

        w.Write("RIFF"u8);
        w.Write(36 + dataSize);
        w.Write("WAVE"u8);

        w.Write("fmt "u8);
        w.Write(16);
        w.Write((short)1); // PCM
        w.Write((short)channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write((short)blockAlign);
        w.Write((short)bitsPerSample);

        w.Write("data"u8);
        w.Write(dataSize);
        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1f, 1f);
            w.Write((short)(clamped * short.MaxValue));
        }

        w.Flush();
        return stream.ToArray();
    }
}
