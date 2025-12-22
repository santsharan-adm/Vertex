using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IPCSoftware.App.Converters
{
    /// Converts a boolean value to "Yes" or "No" string and vice versa.
    /// 
    /// true  -> "Yes"
    /// false -> "No"
    
    public class BooleanToYesNoConverter : IValueConverter
    {
        /// Converts a boolean value to a string ("Yes" / "No").
        /// Called automatically by WPF binding engine.
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            // Check if incoming value is a boolean
            if (value is bool boolValue)
                return boolValue ? "Yes" : "No";


            return "No";
        }

        /// Converts a string ("Yes" / "No") back to boolean.
        /// Useful for TwoWay bindings.

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if incoming value is a string

            if (value is string strValue)

                // Case-insensitive comparison with "Yes"
                return strValue.Equals("Yes", StringComparison.OrdinalIgnoreCase);

            // Fallback if value is null or not string

            return false;
        }
    }


   


}
