using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace IPCSoftware.App.Converters
{
    /// Converts a result status string into a corresponding color.
    /// Used in WPF bindings to visually represent inspection results.
    public class ResultToColorConverter : IValueConverter
    {
        /// Converts the input value (result string) into a SolidColorBrush.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert the incoming value to string

            string result = value.ToString();

            // Return color based on result value
            return result switch
            {
                // Green color for successful result
                "OK" => new SolidColorBrush(Colors.LimeGreen),
                // Red color for failed result
                "NG" => new SolidColorBrush(Colors.Red),
                // Gray color for unchecked or default state
                "Unchecked" => new SolidColorBrush(Colors.Gray),
                // Fallback color if value is null or unrecognized
                _ => new SolidColorBrush(Colors.Gray),
            };
        }

        /// ConvertBack is not implemented because this converter
        /// is intended for one-way data binding only.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
