using System.Windows;
using WhisperFlowLocal.ViewModels;

namespace WhisperFlowLocal.Windows;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm        = vm;
        DataContext = vm;
    }

    private void OnInsightsClick(object sender, RoutedEventArgs e) => _vm.ShowInsights();
    private void OnSettingsClick(object sender, RoutedEventArgs e) => _vm.ShowSettings();
}
