using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.ViewModels;

namespace WhisperFlowLocal.Windows;

public partial class DynamicIslandWindow : Window
{
    private readonly DynamicIslandViewModel _vm;
    private readonly DispatcherTimer _waveformTimer;
    private readonly Storyboard _spinnerAnim;
    private Rectangle[] _bars = [];
    private DispatcherTimer? _errorDismissTimer;

    public DynamicIslandWindow(DynamicIslandViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        _waveformTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _waveformTimer.Tick += OnWaveformTick;

        // Clone so the resource Storyboard is independently controllable
        _spinnerAnim = ((Storyboard)FindResource("SpinnerAnim")).Clone();

        _bars = [Bar0, Bar1, Bar2, Bar3, Bar4, Bar5, Bar6, Bar7, Bar8, Bar9];
        vm.PropertyChanged += OnVmPropertyChanged;
        SystemEvents.DisplaySettingsChanged += (_, _) => Dispatcher.Invoke(PositionBottomCenter);

        PositionBottomCenter();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DynamicIslandViewModel.State))
            Dispatcher.Invoke(UpdateVisualState);
        if (e.PropertyName == nameof(DynamicIslandViewModel.ErrorMessage) &&
            _vm.State == RecordingState.Error)
            Dispatcher.Invoke(() => ErrorContent.Text = _vm.ErrorMessage);
    }

    private void UpdateVisualState()
    {
        switch (_vm.State)
        {
            case RecordingState.Idle:
                _waveformTimer.Stop();
                _spinnerAnim.Stop(this);
                HideCapsule();
                break;

            case RecordingState.Recording:
                RecordingContent.Visibility    = Visibility.Visible;
                TranscribingContent.Visibility = Visibility.Collapsed;
                ErrorContent.Visibility        = Visibility.Collapsed;
                ShowCapsule(200);
                _waveformTimer.Start();
                _spinnerAnim.Stop(this);
                break;

            case RecordingState.Transcribing:
            case RecordingState.Inserting:
                RecordingContent.Visibility    = Visibility.Collapsed;
                TranscribingContent.Visibility = Visibility.Visible;
                ErrorContent.Visibility        = Visibility.Collapsed;
                ShowCapsule(160);
                _waveformTimer.Stop();
                _spinnerAnim.Begin(this, true);
                break;

            case RecordingState.Error:
                ErrorContent.Text              = _vm.ErrorMessage;
                RecordingContent.Visibility    = Visibility.Collapsed;
                TranscribingContent.Visibility = Visibility.Collapsed;
                ErrorContent.Visibility        = Visibility.Visible;
                ShowCapsule(200);
                _waveformTimer.Stop();
                _spinnerAnim.Stop(this);
                ScheduleErrorDismiss();
                break;
        }
    }

    private void ShowCapsule(double targetWidth)
    {
        double fromWidth = Capsule.Visibility == Visibility.Collapsed ? 0 : Capsule.ActualWidth;
        Capsule.Visibility = Visibility.Visible;
        var anim = new DoubleAnimation(fromWidth, targetWidth, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Capsule.BeginAnimation(WidthProperty, anim);
    }

    private void HideCapsule()
    {
        if (Capsule.Visibility == Visibility.Collapsed) return;
        var anim = new DoubleAnimation(Capsule.ActualWidth, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) => Capsule.Visibility = Visibility.Collapsed;
        Capsule.BeginAnimation(WidthProperty, anim);
    }

    private void ScheduleErrorDismiss()
    {
        _errorDismissTimer?.Stop();
        _errorDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _errorDismissTimer.Tick += (_, _) =>
        {
            _errorDismissTimer!.Stop();
            if (_vm.State == RecordingState.Error) HideCapsule();
        };
        _errorDismissTimer.Start();
    }

    private void OnWaveformTick(object? sender, EventArgs e)
    {
        foreach (var bar in _bars)
            bar.Height = Random.Shared.Next(4, 17);
    }

    private void PositionBottomCenter()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Left + (wa.Width - Width) / 2;
        Top  = wa.Bottom - Height - 24;
    }
}
