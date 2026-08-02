using System.Globalization;
using System.Windows.Data;

namespace WhisperFlowLocal.Converters;

public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double pct && parameter is string s && double.TryParse(s, out double max))
            return Math.Max(0, pct / 100.0 * max);
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
