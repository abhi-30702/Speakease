using System.IO;
using System.Security.Cryptography;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;
using WhisperFlowLocal.Models;

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
            await WriteHashSidecarAsync(_modelPath);
            progress?.Report("Model downloaded.");
        }
        else
        {
            await VerifyIntegrityAsync(_modelPath, progress);
        }

        _factory = WhisperFactory.FromPath(_modelPath);
        _processor = _factory.CreateBuilder()
            .WithLanguage("en")
            .WithProbabilities()
            .Build();
    }

    private static async Task VerifyIntegrityAsync(string modelPath, IProgress<string>? progress)
    {
        var hashPath = modelPath + ".sha256";

        if (!File.Exists(hashPath))
        {
            // First run after this update: record a baseline hash for future checks
            await WriteHashSidecarAsync(modelPath);
            return;
        }

        var storedHash  = (await File.ReadAllTextAsync(hashPath)).Trim();
        var currentHash = await ComputeSha256Async(modelPath);

        if (!string.Equals(currentHash, storedHash, StringComparison.OrdinalIgnoreCase))
        {
            // Model file has been tampered with — delete both and re-download
            progress?.Report("Model integrity check failed — re-downloading...");
            File.Delete(modelPath);
            File.Delete(hashPath);
            await DownloadModelAsync(modelPath);
            await WriteHashSidecarAsync(modelPath);
        }
    }

    private static async Task WriteHashSidecarAsync(string modelPath)
    {
        var hash = await ComputeSha256Async(modelPath);
        await File.WriteAllTextAsync(modelPath + ".sha256", hash);
    }

    private static async Task<string> ComputeSha256Async(string path)
    {
        await using var fs = File.OpenRead(path);
        var bytes = await SHA256.HashDataAsync(fs);
        return Convert.ToHexString(bytes).ToLowerInvariant();
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

    public async Task<TranscriptionResult> TranscribeAsync(MemoryStream pcmStream)
    {
        if (_processor is null)
            throw new InvalidOperationException("Call InitializeAsync first.");

        var wav = WavHelper.WrapInWav(pcmStream);
        var sb = new StringBuilder();
        double totalProb = 0;
        int segCount = 0;
        await foreach (var segment in _processor.ProcessAsync(wav))
        {
            sb.Append(segment.Text).Append(' ');
            totalProb += segment.Probability;
            segCount++;
        }
        double avgConf = segCount > 0 ? totalProb / segCount : 0;
        return new TranscriptionResult(sb.ToString().Trim(), avgConf);
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor is not null) { await _processor.DisposeAsync(); _processor = null; }
        _factory?.Dispose(); _factory = null;
    }
}
