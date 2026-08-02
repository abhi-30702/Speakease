using System.Windows;
using System.Windows.Media.Animation;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.Windows;

public partial class OnboardingWindow : Window
{
    private readonly TranscriptionService _transcription;
    private readonly SettingsService _settingsService;
    private int _step = 1;
    private bool _modelReady;
    private bool _downloadInProgress;

    // 560 px window width: 25 / 50 / 75 / 100 %
    private static readonly double[] ProgressWidths = [140, 280, 420, 560];

    public OnboardingWindow(TranscriptionService transcription, SettingsService settingsService)
    {
        InitializeComponent();
        _transcription  = transcription;
        _settingsService = settingsService;
        ShowStep(1);
    }

    private void ShowStep(int step)
    {
        _step = step;

        Step1Panel.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        Step4Panel.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.Visibility = step > 1 ? Visibility.Visible : Visibility.Collapsed;
        BackButton.IsEnabled  = !(step == 2 && _downloadInProgress);
        NextButton.Content    = step == 1 ? "Get started →" : step == 4 ? "Done" : "Next →";
        NextButton.IsEnabled  = step != 2 || _modelReady;

        var anim = new DoubleAnimation(ProgressFill.ActualWidth, ProgressWidths[step - 1],
            TimeSpan.FromMilliseconds(300));
        ProgressFill.BeginAnimation(WidthProperty, anim);
    }

    private async void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_step == 4)
        {
            _settingsService.Current.HasCompletedOnboarding = true;
            _settingsService.Save();
            Close();
            return;
        }
        if (_step == 1)
        {
            ShowStep(2);
            if (!_downloadInProgress)
                await StartModelDownloadAsync();
            return;
        }
        ShowStep(_step + 1);
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_step > 1) ShowStep(_step - 1);
    }

    private async Task StartModelDownloadAsync()
    {
        _downloadInProgress          = true;
        DownloadProgress.Visibility  = Visibility.Visible;
        DownloadErrorPanel.Visibility = Visibility.Collapsed;
        NextButton.IsEnabled         = false;

        // Progress<T> captures the UI sync context — no Dispatcher.Invoke needed
        var progress = new Progress<string>(msg => DownloadStatus.Text = msg);

        try
        {
            await _transcription.InitializeAsync(progress);
            if (!IsLoaded) return;
            DownloadProgress.Visibility = Visibility.Collapsed;
            DownloadStatus.Text         = "Model ready ✓";
            _modelReady                 = true;
            NextButton.IsEnabled        = true;
        }
        catch (Exception ex)
        {
            if (!IsLoaded) return;
            DownloadProgress.Visibility  = Visibility.Collapsed;
            DownloadErrorText.Text       = ex.Message;
            DownloadErrorPanel.Visibility = Visibility.Visible;
        }
        finally
        {
            _downloadInProgress   = false;
            BackButton.IsEnabled  = true;
        }
    }

    private async void OnRetryClick(object sender, RoutedEventArgs e)
    {
        if (_downloadInProgress) return;
        DownloadErrorPanel.Visibility = Visibility.Collapsed;
        await StartModelDownloadAsync();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnDragHandle(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }
}
