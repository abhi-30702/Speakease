using CommunityToolkit.Mvvm.ComponentModel;

namespace WhisperFlowLocal.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private object _currentView;

    public InsightsViewModel Insights { get; }
    public SettingsViewModel Settings  { get; }

    public MainViewModel(InsightsViewModel insights, SettingsViewModel settings)
    {
        Insights     = insights;
        Settings     = settings;
        _currentView = insights;
    }

    public void ShowInsights() => CurrentView = Insights;
    public void ShowSettings() => CurrentView = Settings;
}
