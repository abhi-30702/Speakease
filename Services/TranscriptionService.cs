using System.IO;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;

namespace WhisperFlowLocal.Services;

public class TranscriptionService : IAsyncDisposable
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
        if (_processor is not null) return;

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
        var tmpPath = destPath + ".tmp";
        try
        {
            using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.SmallEn);
            await using var fs = File.Create(tmpPath);
            await modelStream.CopyToAsync(fs);
            File.Move(tmpPath, destPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            throw;
        }
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

    public async ValueTask DisposeAsync()
    {
        if (_processor is not null) { await _processor.DisposeAsync(); _processor = null; }
        _factory?.Dispose(); _factory = null;
    }
}
