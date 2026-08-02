using System.Windows;
using WhisperFlowLocal.ViewModels;

namespace WhisperFlowLocal.Views;

public partial class InsightsView : System.Windows.Controls.UserControl
{
    public InsightsView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InsightsViewModel vm)
            _ = vm.RefreshAsync();
    }
}
