using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    [ObservableProperty] private string _groqApiKey = string.Empty;
    [ObservableProperty] private string _selectedModel = "llama-3.3-70b-versatile";
    [ObservableProperty] private string _saveStatus = string.Empty;

    public string[] AvailableModels { get; } =
        ["llama-3.3-70b-versatile", "llama3-8b-8192", "mixtral-8x7b-32768"];

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        GroqApiKey    = settings.Current.GroqApiKey;
        SelectedModel = settings.Current.GroqModel;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.Current.GroqApiKey = GroqApiKey;
        _settings.Current.GroqModel  = SelectedModel;
        _settings.Save();
        SaveStatus = "Saved ✓";
    }
}
