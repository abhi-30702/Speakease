using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
    private DynamicIslandWindow? _pillWindow;
    private Windows.MainWindow? _mainWindow;
    private MainViewModel? _mainVm;
    private InsightsViewModel? _insightsVm;
    private InsightsRepository? _insights;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Settings
        var settingsService = new SettingsService();
        settingsService.Load();

        // Insights DB
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WhisperFlowLocal", "insights.db");
        _insights = new InsightsRepository(dbPath);
        await _insights.InitAsync();

        // Core services
        var focus      = new FocusService();
        var audio      = new AudioCaptureService();
        var modelPath  = Path.Combine(AppContext.BaseDirectory, "Resources", "Models", "ggml-small.en.bin");
        var transcription = new TranscriptionService(modelPath);
        var regex      = new RegexCleanupService();
        var groq       = new GroqCleanupService(new HttpClient(), settingsService);
        var cleanup    = new TieredCleanupService(groq, regex);
        var insertion  = new InsertionService(focus);

        // First-run onboarding (ShowDialog blocks until window closes)
        if (!settingsService.Current.HasCompletedOnboarding)
        {
            var onboarding = new OnboardingWindow(transcription, settingsService);
            onboarding.ShowDialog();
            if (!settingsService.Current.HasCompletedOnboarding)
            {
                // User closed before finishing — exit without starting the app
                Shutdown();
                return;
            }
        }

        // Tray + model load (InitializeAsync is a no-op when model is already loaded from onboarding)
        SetupTray();
        _trayIcon!.ShowBalloonTip(3000, "Whisper Flow", "Loading speech model...", ToolTipIcon.Info);
        await transcription.InitializeAsync();
        _trayIcon.ShowBalloonTip(2000, "Whisper Flow", "Ready. Hold Ctrl+Space to dictate.", ToolTipIcon.Info);

        // Engine
        _engine = new DictationEngine(audio, transcription, cleanup, insertion, focus, _insights);

        // Dynamic Island pill (transparent at idle — capsule is Collapsed)
        var pillVm = new DynamicIslandViewModel();
        pillVm.SyncFrom(_engine);
        _pillWindow = new DynamicIslandWindow(pillVm);
        _pillWindow.Show();

        // ViewModels
        _insightsVm = new InsightsViewModel(_insights);
        var settingsVm = new SettingsViewModel(settingsService);
        _mainVm = new MainViewModel(_insightsVm, settingsVm);

        // Refresh Insights after each dictation (marshal to UI thread)
        _engine.DictationCompleted += () =>
            Dispatcher.BeginInvoke(() => _ = _insightsVm.RefreshAsync());

        // Keyboard hook
        InstallHook();

        // Update check (fire-and-forget; never blocks startup)
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        var (available, tag) = await UpdateChecker.CheckAsync();
        if (!available) return;

        _trayIcon!.BalloonTipClicked += OpenReleasesPage;
        _trayIcon.ShowBalloonTip(
            6000,
            "Update available",
            $"Whisper Flow {tag} is out. Click to download.",
            ToolTipIcon.Info);
    }

    private void OpenReleasesPage(object? sender, EventArgs e)
    {
        _trayIcon!.BalloonTipClicked -= OpenReleasesPage;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "https://github.com/abhi-30702/Speakease/releases/latest")
        { UseShellExecute = true });
    }

    private void SetupTray()
    {
        var iconUri    = new Uri("pack://application:,,,/Resources/tray-icon.ico");
        var iconStream = GetResourceStream(iconUri)?.Stream;

        _trayIcon = new NotifyIcon
        {
            Icon    = iconStream != null ? new System.Drawing.Icon(iconStream) : SystemIcons.Application,
            Visible = true,
            Text    = "Whisper Flow Local"
        };

        var menu       = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("Toggle Mode") { CheckOnClick = true };
        toggleItem.CheckedChanged += (_, _) =>
        {
            if (_engine != null) _engine.ToggleMode = toggleItem.Checked;
        };
        menu.Items.Add(toggleItem);

        var startupItem = new ToolStripMenuItem("Start on login")
        {
            CheckOnClick = true,
            Checked      = StartupService.IsEnabled()
        };
        startupItem.CheckedChanged += (_, _) =>
        {
            if (startupItem.Checked) StartupService.Enable();
            else                     StartupService.Disable();
        };
        menu.Items.Add(startupItem);

        menu.Items.Add("Settings", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Quit",     null, (_, _) => Shutdown());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick     += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new Windows.MainWindow(_mainVm!);
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void InstallHook()
    {
        _hookProc = LowLevelKeyboardHook;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule  = curProcess.MainModule!;
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
            var kb       = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            bool isSpace  = kb.vkCode == NativeMethods.VK_SPACE;
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
        _insights?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
