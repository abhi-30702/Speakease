using System.IO;

namespace WhisperFlowLocal.Services;

public static class WavHelper
{
    public static MemoryStream WrapInWav(MemoryStream pcm)
    {
        pcm.Position = 0;
        var wav = new MemoryStream();
        var writer = new BinaryWriter(wav);
        const int sampleRate = 16000, channels = 1, bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int dataLen = (int)pcm.Length;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLen);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write((short)bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataLen);
        pcm.CopyTo(wav);
        wav.Position = 0;
        return wav;
    }
}
