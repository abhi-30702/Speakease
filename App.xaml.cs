using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using WhisperFlowLocal.Interop;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.Services;
using WhisperFlowLocal.ViewModels;
using WhisperFlowLocal.Windows;

namespace WhisperFlowLocal;

public partial class App : System.Windows.Application
{
    private const int HOTKEY_ID = 9001;

    private NotifyIcon? _trayIcon;
    private HwndSource? _hwndSource;
    private DictationEngine? _engine;
    private PillWindow? _pillWindow;
    private WhisperFlowLocal.Windows.MainWindow? _mainWindow;

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

        // Global hotkey via hidden window
        RegisterHotkey();
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
        menu.Items.Add("Settings", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Quit", null, (_, _) => Shutdown());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        _mainWindow ??= new WhisperFlowLocal.Windows.MainWindow();
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void RegisterHotkey()
    {
        // Hidden helper window to receive WM_HOTKEY messages
        var helper = new Window { Width = 0, Height = 0, WindowStyle = WindowStyle.None, ShowInTaskbar = false };
        helper.Show();
        helper.Hide();

        var handle = new WindowInteropHelper(helper).EnsureHandle();
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource.AddHook(WndProc);

        NativeMethods.RegisterHotKey(handle, HOTKEY_ID, NativeMethods.MOD_CONTROL, NativeMethods.VK_SPACE);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            // lParam bit 31: 0 = key down, 1 = key up
            bool isKeyUp = ((lParam.ToInt64() >> 31) & 1) == 1;
            if (!isKeyUp) _engine?.OnHotkeyPressed();
            else          _engine?.OnHotkeyReleased();
            handled = true;
        }
        return IntPtr.Zero;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_hwndSource != null)
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID);
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
