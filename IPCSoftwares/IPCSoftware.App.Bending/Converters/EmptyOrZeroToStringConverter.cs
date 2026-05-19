using System;
using System.Globalization;
using System.Windows.Data;

namespace IPCSoftware.App.Bending.Converters
{
    /// <summary>
    /// Converts empty strings, null values, and zero numeric values to a display string ("-")
    /// </summary>
    public class EmptyOrZeroToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Handle null values
            if (value == null)
                return "-";

            // Handle empty strings
            if (value is string strValue && string.IsNullOrWhiteSpace(strValue))
                return "-";

            // Return the original value as string (including zeros)
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
