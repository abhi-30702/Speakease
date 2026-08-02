using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
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
        {
            GroqApiKeyBox.Password   = vm.GroqApiKey;
            OpenAiApiKeyBox.Password = vm.OpenAiApiKey;
        }
    }

    private void OnGroqKeyChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.GroqApiKey = GroqApiKeyBox.Password;
    }

    private void OnOpenAiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.OpenAiApiKey = OpenAiApiKeyBox.Password;
    }

    private void OnHyperlinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
