using WhisperFlowLocal.Interop;

namespace WhisperFlowLocal.Services;

public class FocusService
{
    private IntPtr _capturedHwnd = IntPtr.Zero;

    public void CaptureForegroudWindow()
        => _capturedHwnd = NativeMethods.GetForegroundWindow();

    public void RestoreFocus()
    {
        if (_capturedHwnd == IntPtr.Zero) return;
        NativeMethods.SetForegroundWindow(_capturedHwnd);
        Thread.Sleep(50);
    }

    public string GetForegroundAppName()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return string.Empty;
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        try { return System.Diagnostics.Process.GetProcessById((int)pid).ProcessName; }
        catch { return string.Empty; }
    }

    public string GetForegroundWindowTitle()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return string.Empty;
        var sb = new System.Text.StringBuilder(256);
        NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
