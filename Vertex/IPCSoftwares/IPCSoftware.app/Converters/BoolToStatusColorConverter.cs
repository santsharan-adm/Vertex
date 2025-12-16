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

    /// <summary>
    /// Converts a boolean (or similar) value into a status color.
    /// - True / ON / 1 → Green
    /// - False / OFF / 0 → Red
    /// 
    /// Supports multiple input types: bool, int, and string.
    /// Commonly used to color UI elements based on system status.
    /// </summary>
    public class BoolToStatusColorConverter : IValueConverter
    {
        /// Converts a value (bool/int/string) into a SolidColorBrush.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If value is null, return a neutral color
            if (value == null) return Brushes.LightGray;

            bool isOn = false;                  // Default state is OFF

            // Handle boolean values directly
            if (value is bool b) isOn = b;
            // Handle integer values (any number > 0 is treated as ON)
            else if (value is int i) isOn = i > 0;
            // Handle string values ("true"/"1" → ON, "false"/"0" → OFF)
            else if (value is string s)
            {
                if (bool.TryParse(s, out bool res)) isOn = res;
                else if (int.TryParse(s, out int ires)) isOn = ires > 0;
            }

            // Return color based on state:
            // Green → ON, Red → OFF
            return isOn ? new SolidColorBrush(Color.FromRgb(40, 167, 69))  // Green
                : new SolidColorBrush(Color.FromRgb(220, 53, 69));          // Red
        }

        /// Reverse conversion is not implemented (color → bool).

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
