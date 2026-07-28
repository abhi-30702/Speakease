using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WhisperFlowLocal.Models;

namespace WhisperFlowLocal.Converters;

public class StateToVisibilityConverter : IValueConverter
{
    public RecordingState TargetState { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is RecordingState s && s == TargetState
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
