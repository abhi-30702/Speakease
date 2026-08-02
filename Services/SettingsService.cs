using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Services;

public class SettingsService
{
    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Speakease", "settings.json");

    private readonly string _path;
    public AppSettings Current { get; private set; } = new();

    public SettingsService(string? path = null) => _path = path ?? DefaultPath;

    public void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            var stored = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new();
            Current = new AppSettings
            {
                GroqApiKey             = DpapiDecrypt(stored.GroqApiKey),
                GroqModel              = stored.GroqModel,
                HasCompletedOnboarding = stored.HasCompletedOnboarding,
            };
        }
        catch { Current = new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var toWrite = new AppSettings
        {
            GroqApiKey             = DpapiEncrypt(Current.GroqApiKey),
            GroqModel              = Current.GroqModel,
            HasCompletedOnboarding = Current.HasCompletedOnboarding,
        };
        File.WriteAllText(_path, JsonSerializer.Serialize(toWrite, new JsonSerializerOptions { WriteIndented = true }));
    }

    // DPAPI: encrypted by Windows for this user account only; unreadable by other users or on other machines
    private static string DpapiEncrypt(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        try
        {
            var cipher = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cipher);
        }
        catch { return value; }
    }

    private static string DpapiDecrypt(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        try
        {
            var plain = ProtectedData.Unprotect(
                Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch { return value; } // migration: if stored as plain text, return as-is; next Save() will encrypt it
    }
}
