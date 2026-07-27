using System.IO;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;

namespace WhisperFlowLocal.Services;

public class TranscriptionService : IDisposable
{
    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;
    private readonly string _modelPath;

    public TranscriptionService(string modelPath)
    {
        _modelPath = modelPath;
    }

    public async Task InitializeAsync(IProgress<string>? progress = null)
    {
        if (!File.Exists(_modelPath))
        {
            progress?.Report("Downloading small.en model (~465 MB)...");
            await DownloadModelAsync(_modelPath);
            progress?.Report("Model downloaded.");
        }
        _factory = WhisperFactory.FromPath(_modelPath);
        _processor = _factory.CreateBuilder()
            .WithLanguage("en")
            .Build();
    }

    private static async Task DownloadModelAsync(string destPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.SmallEn);
        await using var fs = File.Create(destPath);
        await modelStream.CopyToAsync(fs);
    }

    public async Task<string> TranscribeAsync(MemoryStream pcmStream)
    {
        if (_processor is null)
            throw new InvalidOperationException("Call InitializeAsync first.");

        var wav = WavHelper.WrapInWav(pcmStream);
        var sb = new StringBuilder();
        await foreach (var segment in _processor.ProcessAsync(wav))
            sb.Append(segment.Text).Append(' ');
        return sb.ToString().Trim();
    }

    public void Dispose()
    {
        _processor?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _factory?.Dispose();
    }
}
