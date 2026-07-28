using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using WhisperFlowLocal.Interop;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.Services;
using WhisperFlowLocal.ViewModels;
using WhisperFlowLocal.Windows;

namespace WhisperFlowLocal;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;
    private IntPtr _hookHandle = IntPtr.Zero;
    private NativeMethods.LowLevelKeyboardProc? _hookProc;
    private DictationEngine? _engine;
    private PillWindow? _pillWindow;
    private Windows.MainWindow? _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build services
        var focus = new FocusService();
        var audio = new AudioCaptureService();
        var modelPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "Models", "ggml-small.en.bin");
        var transcription = new TranscriptionService(modelPath);
        var cleanup = new CleanupService();
        var insertion = new InsertionService(focus);

        // Tray + loading balloon
        SetupTray();
        _trayIcon!.ShowBalloonTip(3000, "Whisper Flow", "Loading speech model...", ToolTipIcon.Info);
        await transcription.InitializeAsync();
        _trayIcon.ShowBalloonTip(2000, "Whisper Flow", "Ready. Hold Ctrl+Space to dictate.", ToolTipIcon.Info);

        // Engine + pill
        _engine = new DictationEngine(audio, transcription, cleanup, insertion, focus);
        var pillVm = new PillViewModel();
        pillVm.SyncFrom(_engine);
        _pillWindow = new PillWindow(pillVm);

        // Low-level keyboard hook for Ctrl+Space
        InstallHook();
    }

    private void SetupTray()
    {
        var iconUri = new Uri("pack://application:,,,/Resources/tray-icon.ico");
        var iconStream = GetResourceStream(iconUri)?.Stream;

        _trayIcon = new NotifyIcon
        {
            Icon = iconStream != null ? new System.Drawing.Icon(iconStream) : SystemIcons.Application,
            Visible = true,
            Text = "Whisper Flow Local"
        };

        var menu = new ContextMenuStrip();

        var toggleItem = new ToolStripMenuItem("Toggle Mode") { CheckOnClick = true };
        toggleItem.CheckedChanged += (_, _) =>
        {
            if (_engine != null) _engine.ToggleMode = toggleItem.Checked;
        };
        menu.Items.Add(toggleItem);
        menu.Items.Add("Settings", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Quit", null, (_, _) => Shutdown());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        _mainWindow ??= new Windows.MainWindow();
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void InstallHook()
    {
        _hookProc = LowLevelKeyboardHook;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);
    }

    private IntPtr LowLevelKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            bool isSpace = kb.vkCode == NativeMethods.VK_SPACE;
            bool ctrlDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;

            if (isSpace && ctrlDown)
            {
                int msg = wParam.ToInt32();
                if (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN)
                    Dispatcher.BeginInvoke(() => _engine?.OnHotkeyPressed());
                else if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
                    Dispatcher.BeginInvoke(() => _engine?.OnHotkeyReleased());
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
