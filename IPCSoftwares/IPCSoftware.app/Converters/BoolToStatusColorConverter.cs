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

    /// Converts different boolean-like values into a status color.
    /// true / 1 / "true" / "1"  -> Green (ON)
    /// false / 0 / "false" / "0" -> Red (OFF)
    /// null -> LightGray
    public class BoolToStatusColorConverter : IValueConverter
    {
        /// Converts input value into a Brush representing ON/OFF status.

        /// Input value which can be:
        /// bool, int, string ("true"/"false"/"1"/"0")
        /// 
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If value is null, return a neutral color

            if (value == null) return Brushes.LightGray;

            bool isOn = false;                           // Default OFF state

            // Handle boolean input

            if (value is bool b) isOn = b;

            // Handle integer input (1 = ON, 0 = OFF)

            else if (value is int i) isOn = i > 0;

            // Handle string input ("true"/"false"/"1"/"0")
            else if (value is string s)
            {
                if (bool.TryParse(s, out bool res)) isOn = res;
                else if (int.TryParse(s, out int ires)) isOn = ires > 0;
            }

            // Green for ON, Red (or dark gray) for OFF
            return isOn ? new SolidColorBrush(Color.FromRgb(40, 167, 69)) : new SolidColorBrush(Color.FromRgb(220, 53, 69));
        }

        /// ConvertBack is not implemented because this converter
        /// is intended for one-way bindings only.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
