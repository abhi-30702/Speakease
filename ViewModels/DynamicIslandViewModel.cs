using CommunityToolkit.Mvvm.ComponentModel;
using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.ViewModels;

public partial class DynamicIslandViewModel : ObservableObject
{
    [ObservableProperty] private RecordingState _state = RecordingState.Idle;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public void SyncFrom(DictationEngine engine)
    {
        engine.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DictationEngine.State))
                State = engine.State;
            if (e.PropertyName == nameof(DictationEngine.ErrorMessage))
                ErrorMessage = engine.ErrorMessage;
        };
    }
}
