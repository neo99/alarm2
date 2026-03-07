using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AlarmApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRinging && isRinging)
        {
            return new SolidColorBrush(Colors.OrangeRed);
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
