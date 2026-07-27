using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;

namespace WhisperFlowLocal;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        System.Drawing.Icon? icon = null;
        try
        {
            var stream = GetResourceStream(new Uri("Resources/tray-icon.ico", UriKind.Relative))?.Stream;
            if (stream != null) icon = new System.Drawing.Icon(stream);
        }
        catch (Exception ex) { Debug.WriteLine($"Failed to load tray icon: {ex.Message}"); }

        _trayIcon = new NotifyIcon
        {
            Icon = icon ?? SystemIcons.Application,
            Visible = true,
            Text = "Whisper Flow Local"
        };
        _trayIcon.DoubleClick += (_, _) => Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
