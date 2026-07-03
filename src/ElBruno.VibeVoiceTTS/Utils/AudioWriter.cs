using System.Buffers.Binary;

namespace ElBruno.VibeVoiceTTS.Utils;

/// <summary>
/// Writes audio samples to WAV files (RIFF format, 16-bit PCM).
/// </summary>
public static class AudioWriter
{
    /// <summary>
    /// Saves float audio samples as a 16-bit PCM WAV file.
    /// </summary>
    public static void SaveWav(string path, float[] samples, int sampleRate = 24000, int channels = 1)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(path);
        if (samples.Length == 0)
            throw new ArgumentException("Audio samples array is empty.", nameof(samples));

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(path, GetWavBytes(samples, sampleRate, channels));
    }

    /// <summary>
    /// Serializes float audio samples into a 16-bit PCM WAV payload.
    /// </summary>
    public static byte[] GetWavBytes(ReadOnlyMemory<float> samples, int sampleRate = 24000, int channels = 1)
    {
        const int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize = samples.Length * blockAlign;

        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        writer.Write("data"u8);
        writer.Write(dataSize);

        ReadOnlySpan<float> sampleSpan = samples.Span;
        for (int i = 0; i < sampleSpan.Length; i++)
        {
            writer.Write(ToPcm16(sampleSpan[i]));
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Serializes float audio samples into signed 16-bit PCM little-endian bytes.
    /// </summary>
    public static byte[] GetPcm16LeBytes(ReadOnlyMemory<float> samples)
    {
        byte[] bytes = new byte[samples.Length * sizeof(short)];
        Span<byte> destination = bytes.AsSpan();
        int offset = 0;

        ReadOnlySpan<float> sampleSpan = samples.Span;
        for (int i = 0; i < sampleSpan.Length; i++)
        {
            short pcmSample = ToPcm16(sampleSpan[i]);
            BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(offset, sizeof(short)), pcmSample);
            offset += sizeof(short);
        }

        return bytes;
    }

    /// <summary>
    /// Serializes float audio samples into IEEE 754 single-precision PCM little-endian bytes.
    /// </summary>
    public static byte[] GetPcmFloat32LeBytes(ReadOnlyMemory<float> samples)
    {
        byte[] bytes = new byte[samples.Length * sizeof(float)];
        Span<byte> destination = bytes.AsSpan();
        ReadOnlySpan<float> source = samples.Span;

        for (int i = 0; i < source.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(
                destination.Slice(i * sizeof(float), sizeof(float)),
                source[i]);
        }

        return bytes;
    }

    private static short ToPcm16(float sample)
    {
        float clamped = Math.Clamp(sample, -1.0f, 1.0f);
        return (short)Math.Round(clamped * short.MaxValue);
    }
}
