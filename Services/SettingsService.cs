using System.IO;
using System.Text.Json;
using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Services;

public class SettingsService
{
    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WhisperFlowLocal", "settings.json");

    private readonly string _path;
    public AppSettings Current { get; private set; } = new();

    public SettingsService(string? path = null) => _path = path ?? DefaultPath;

    public void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new();
        }
        catch { Current = new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
    }
}
