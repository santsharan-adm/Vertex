using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IPCSoftware.App.Bending.Converters
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    internal sealed class BindingManualPageVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            // If already a Visibility, return as-is
            if (value is Visibility vis)
                return vis;

            // Boolean -> Visible/Collapsed
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            if (value is int bi)
            {
                if (parameter is int param)
                {
                    return bi == param ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    if(int.TryParse(parameter.ToString(), out int param1))
                    {
                        return bi == param1 ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }

            // String handling
            if (value is string s)
            {
                if (parameter is string param && !string.IsNullOrEmpty(param))
                {
                    return string.Equals(s, param, StringComparison.OrdinalIgnoreCase)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                return s.IndexOf("manual", StringComparison.OrdinalIgnoreCase) >= 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            // Enum handling
            var valueType = value.GetType();
            if (valueType.IsEnum)
            {
                var enumName = value.ToString() ?? string.Empty;
                if (parameter is string param && !string.IsNullOrEmpty(param))
                {
                    return string.Equals(enumName, param, StringComparison.OrdinalIgnoreCase)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                return enumName.IndexOf("manual", StringComparison.OrdinalIgnoreCase) >= 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            // Fallback
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not supported for this converter
            throw new NotSupportedException($"{nameof(BindingManualPageVisibilityConverter)} does not support ConvertBack.");
        }
    }
}
