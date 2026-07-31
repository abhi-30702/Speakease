using System.IO;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tmpPath = Path.Combine(Path.GetTempPath(), $"wfl-test-{Guid.NewGuid()}.json");
    private SettingsService Svc() => new(_tmpPath);

    [Fact]
    public void Load_WhenFileAbsent_ReturnsDefaults()
    {
        var svc = Svc();
        svc.Load();
        Assert.Equal(string.Empty, svc.Current.GroqApiKey);
        Assert.Equal("llama-3.3-70b-versatile", svc.Current.GroqModel);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var svc = Svc();
        svc.Current.GroqApiKey = "gsk_test";
        svc.Current.GroqModel = "llama3-8b-8192";
        svc.Save();

        var svc2 = Svc();
        svc2.Load();
        Assert.Equal("gsk_test", svc2.Current.GroqApiKey);
        Assert.Equal("llama3-8b-8192", svc2.Current.GroqModel);
    }

    public void Dispose() { if (File.Exists(_tmpPath)) File.Delete(_tmpPath); }
}
