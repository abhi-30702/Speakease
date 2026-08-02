using Microsoft.Win32;

namespace WhisperFlowLocal.Services;

public static class StartupService
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WhisperFlow";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) != null;
    }

    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)!;
        key.SetValue(AppName, $"\"{Environment.ProcessPath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)!;
        key.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
