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
}
