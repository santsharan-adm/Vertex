using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IPCSoftware.App.Bending.Converters
{
    /// <summary>
    /// Converts bool to Visibility.
    /// ConverterParameter=Inverse reverses the mapping.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            bool inverse = parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
            if (inverse) boolValue = !boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = value is Visibility v && v == Visibility.Visible;
            bool inverse = parameter is string s && s.Equals("Inverse", StringComparison.OrdinalIgnoreCase);
            return inverse ? !visible : visible;
        }
    }
}
