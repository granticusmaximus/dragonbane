using DndCompanion.Application.Audio;

namespace DndCompanion.Tests;

public class WavEncoderTests
{
    [Fact]
    public void Encodes_valid_RIFF_WAVE_header_with_correct_sizes()
    {
        float[] samples = [0f, 0.5f, -0.5f, 1f, -1f];
        var bytes = WavEncoder.EncodeMono16Bit(samples, sampleRate: 16_000);

        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal("WAVE", System.Text.Encoding.ASCII.GetString(bytes, 8, 4));
        Assert.Equal("fmt ", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));
        Assert.Equal("data", System.Text.Encoding.ASCII.GetString(bytes, 36, 4));

        var dataSize = BitConverter.ToInt32(bytes, 40);
        Assert.Equal(samples.Length * 2, dataSize); // 16-bit = 2 bytes/sample
        Assert.Equal(44 + dataSize, bytes.Length);

        var riffSize = BitConverter.ToInt32(bytes, 4);
        Assert.Equal(bytes.Length - 8, riffSize);
    }

    [Fact]
    public void Encodes_mono_16bit_pcm_format_fields()
    {
        var bytes = WavEncoder.EncodeMono16Bit([0f], sampleRate: 16_000);

        Assert.Equal(1, BitConverter.ToInt16(bytes, 20)); // audio format = PCM
        Assert.Equal(1, BitConverter.ToInt16(bytes, 22)); // channels = mono
        Assert.Equal(16_000, BitConverter.ToInt32(bytes, 24)); // sample rate
        Assert.Equal(16, BitConverter.ToInt16(bytes, 34)); // bits per sample
    }

    [Theory]
    [InlineData(1f, short.MaxValue)]
    [InlineData(-1f, -short.MaxValue)]
    [InlineData(0f, 0)]
    [InlineData(2f, short.MaxValue)] // clamped
    [InlineData(-2f, -short.MaxValue)] // clamped
    public void Clamps_and_scales_samples_to_16bit_range(float sample, short expected)
    {
        var bytes = WavEncoder.EncodeMono16Bit([sample], sampleRate: 16_000);
        var encoded = BitConverter.ToInt16(bytes, 44); // first (only) sample after the header
        Assert.Equal(expected, encoded);
    }

    [Fact]
    public void Empty_sample_list_produces_header_only_wav()
    {
        var bytes = WavEncoder.EncodeMono16Bit([], sampleRate: 16_000);
        Assert.Equal(44, bytes.Length);
        Assert.Equal(0, BitConverter.ToInt32(bytes, 40));
    }
}
