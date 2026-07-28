using System.Windows;
using System.Windows.Forms;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.ViewModels;

namespace WhisperFlowLocal.Windows;

public partial class PillWindow : Window
{
    private readonly PillViewModel _vm;

    public PillWindow(PillViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.PropertyChanged += OnVmStateChanged;
        PositionBottomCenter();
    }

    private void OnVmStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PillViewModel.State)) return;
        Dispatcher.Invoke(() =>
        {
            switch (_vm.State)
            {
                case RecordingState.Idle:
                    Hide();
                    break;
                case RecordingState.Recording:
                case RecordingState.Transcribing:
                case RecordingState.Error:
                    if (!IsVisible) Show();
                    break;
                case RecordingState.Inserting:
                    _ = FadeOutAsync();
                    break;
            }
        });
    }

    private async Task FadeOutAsync()
    {
        await Task.Delay(600);
        Dispatcher.Invoke(Hide);
    }

    private void PositionBottomCenter()
    {
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
        Left = (screen.Width - Width) / 2;
        Top = screen.Height - Height - 24;
    }
}
