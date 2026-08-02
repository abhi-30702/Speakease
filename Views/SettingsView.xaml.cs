using System.Windows;
using System.Windows.Controls;
using WhisperFlowLocal.ViewModels;

namespace WhisperFlowLocal.Views;

public partial class SettingsView : System.Windows.Controls.UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SettingsViewModel vm)
            ApiKeyBox.Password = vm.GroqApiKey;
    }

    private void OnApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.GroqApiKey = ApiKeyBox.Password;
    }
}
