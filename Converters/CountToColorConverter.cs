using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WhisperFlowLocal.Converters;

public class CountToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int count = value is int c ? c : 0;
        var hex = count switch
        {
            0      => "#1E293B",
            1 or 2 => "#0f5e58",
            <= 5   => "#0d7a72",
            <= 9   => "#0d9488",
            _      => "#2dd4bf"
        };
        return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
