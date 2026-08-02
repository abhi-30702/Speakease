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
            0      => "#242424",
            1 or 2 => "#6b3f6b",
            <= 5   => "#8f4f8f",
            <= 9   => "#b86ab8",
            _      => "#E5BDDF"
        };
        return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
