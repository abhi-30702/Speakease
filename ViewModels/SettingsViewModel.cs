using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhisperFlowLocal.Models;
using WhisperFlowLocal.Services;

namespace WhisperFlowLocal.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    // ── Provider selection ────────────────────────────────────────────────────
    private string _selectedProvider;
    public string SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetProperty(ref _selectedProvider, value))
            {
                OnPropertyChanged(nameof(IsLocalSelected));
                OnPropertyChanged(nameof(IsGroqSelected));
                OnPropertyChanged(nameof(IsOpenAiSelected));
                OnPropertyChanged(nameof(GroqSectionVisibility));
                OnPropertyChanged(nameof(OpenAiSectionVisibility));
            }
        }
    }

    public bool IsLocalSelected
    {
        get => SelectedProvider == "Local";
        set { if (value) SelectedProvider = "Local"; }
    }
    public bool IsGroqSelected
    {
        get => SelectedProvider == "Groq";
        set { if (value) SelectedProvider = "Groq"; }
    }
    public bool IsOpenAiSelected
    {
        get => SelectedProvider == "OpenAI";
        set { if (value) SelectedProvider = "OpenAI"; }
    }

    public Visibility GroqSectionVisibility   => IsGroqSelected   ? Visibility.Visible : Visibility.Collapsed;
    public Visibility OpenAiSectionVisibility => IsOpenAiSelected ? Visibility.Visible : Visibility.Collapsed;

    // ── Groq ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _groqApiKey    = string.Empty;
    [ObservableProperty] private string _selectedGroqModel;

    public string[] GroqModels { get; } =
        ["llama-3.3-70b-versatile", "llama3-8b-8192", "mixtral-8x7b-32768"];

    // ── OpenAI ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _openAiApiKey   = string.Empty;
    [ObservableProperty] private string _selectedOpenAiModel;

    public string[] OpenAiModels { get; } =
        ["gpt-4o-mini", "gpt-4o", "gpt-3.5-turbo"];

    // ── Status ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _saveStatus = string.Empty;

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;

        _selectedProvider   = settings.Current.CleanupProvider.ToString();
        _groqApiKey         = settings.Current.GroqApiKey;
        _selectedGroqModel  = settings.Current.GroqModel;
        _openAiApiKey       = settings.Current.OpenAiApiKey;
        _selectedOpenAiModel = settings.Current.OpenAiModel;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.Current.CleanupProvider = SelectedProvider switch
        {
            "Groq"   => CleanupProvider.Groq,
            "OpenAI" => CleanupProvider.OpenAI,
            _        => CleanupProvider.Local,
        };
        _settings.Current.GroqApiKey    = GroqApiKey;
        _settings.Current.GroqModel     = SelectedGroqModel;
        _settings.Current.OpenAiApiKey  = OpenAiApiKey;
        _settings.Current.OpenAiModel   = SelectedOpenAiModel;
        _settings.Save();
        SaveStatus = "Saved ✓";
    }
}
