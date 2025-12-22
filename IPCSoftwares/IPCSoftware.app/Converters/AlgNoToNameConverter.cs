using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IPCSoftware.App.Converters
{
    /// Converter that converts an Algorithm Number (int)
    /// into a human-readable Algorithm Name (string).

    /// Example:
    /// 1 -> Linear scale
    /// 2 -> FP
    /// 3 -> String
    
    public class AlgNoToNameConverter : IValueConverter
    {

        /// Converts an integer algorithm number into a display string.
        /// Called automatically by WPF binding engine.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the incoming value is an integer

            if (value is int algNo)
            {
                // Convert algorithm number to readable name

                return algNo switch
                {
                    1 => "Linear scale",
                    2 => "FP",
                    3 => "String",
                    _ => "Unknown"                     // Fallback for unsupported values
                };
            }
            // Fallback if value is null or not an integer

            return "Unknown";
        }

        /// Converts a display string back to algorithm number.
        /// Not implemented because this converter is intended
        /// for OneWay binding only.
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not required for display-only bindings

            throw new NotImplementedException();
        }
    }
}
