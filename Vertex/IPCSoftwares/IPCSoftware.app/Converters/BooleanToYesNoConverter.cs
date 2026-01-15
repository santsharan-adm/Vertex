using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IPCSoftware.App.Converters
{

    /// Converts a boolean value (true/false) to a "Yes"/"No" string for UI display,
    /// and converts "Yes"/"No" text back to a boolean when needed.
    public class BooleanToYesNoConverter : IValueConverter
    {

        /// Converts a boolean value to a "Yes" or "No" string.
        /// Used when displaying data in the UI.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the input value is a boolean
            if (value is bool boolValue)
                // If true → "Yes", otherwise → "No"
                return boolValue ? "Yes" : "No";

            // If the input is not a boolean, return "No" by default
            return "No";
        }
        /// Converts a "Yes"/"No" string back to a boolean.
        /// Used when updating data from the UI back to the model.

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the input value is a string
            if (value is string strValue)
                // Return true if the string is "Yes" (case-insensitive)
                return strValue.Equals("Yes", StringComparison.OrdinalIgnoreCase);

            // If not a valid string, default to false
            return false;
        }
    }


   


}
