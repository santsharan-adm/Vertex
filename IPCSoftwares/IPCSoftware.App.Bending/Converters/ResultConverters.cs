using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IPCSoftware.App.Bending.Converters
{
    /// <summary>
    /// Converts bool (Result) to "OK" or "NG" text
    /// </summary>
    public class BoolToResultTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool result)
                return result ? "OK" : "NG";
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts bool (Result) to Green (OK) or Red (NG) brush
    /// </summary>
    public class BoolToResultColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool result)
                return result
                    ? new SolidColorBrush(Color.FromRgb(0, 176, 80))   // #00B050
                    : new SolidColorBrush(Colors.Red);
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
