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

    /// Converts a result status string (e.g. "OK", "NG", "Unchecked") 
    /// into a color brush for UI display.
    /// Commonly used to visually represent test results or validation states.
    public class ResultToColorConverter : IValueConverter
    {
        /// Converts a result string into a corresponding color.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert the input value to string (handles any object type)
            string result = value.ToString();

            // Choose color based on result text
            return result switch
            {
                "OK" => new SolidColorBrush(Colors.LimeGreen),             // ✅ OK → Green
                "NG" => new SolidColorBrush(Colors.Red),                   // ❌ NG (No Good) → Red
                "Unchecked" => new SolidColorBrush(Colors.Gray),           // ⏸ Unchecked → Gray
                _ => new SolidColorBrush(Colors.Gray),                     // Default → Gray
            };
        }

        /// Not implemented because reverse conversion (color → result) is not required.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
