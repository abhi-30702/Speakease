using System.IO;
using NAudio.Wave;

namespace WhisperFlowLocal.Services;

public class AudioCaptureService : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _buffer;

    public void Start()
    {
        _buffer = new MemoryStream();
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(sampleRate: 16000, channels: 1)
        };
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.StartRecording();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
        => _buffer?.Write(e.Buffer, 0, e.BytesRecorded);

    public MemoryStream Stop()
    {
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;
        var result = _buffer ?? new MemoryStream();
        result.Position = 0;
        _buffer = null;
        return result;
    }

    public void Dispose()
    {
        _waveIn?.Dispose();
        _buffer?.Dispose();
    }
}
