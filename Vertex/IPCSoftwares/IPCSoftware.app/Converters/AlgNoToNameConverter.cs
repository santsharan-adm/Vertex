using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IPCSoftware.App.Converters
{
    public class AlgNoToNameConverter : IValueConverter
    {
       
        // Converts the integer value (algorithm number) to a readable name
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the incoming value is an integer
            if (value is int algNo)
            {
                // Match algorithm number to name
                return algNo switch
                {                                   
                    1 => "Linear scale",            // Algorithm type 1
                    2 => "FP",                      // Algorithm type 2
                    3 => "String",                  // Algorithm type 3
                    _ => "Unknown"                  // Fallback for any undefined number
                };
            }
            // If value is null or not an integer, return "Unknown"
            return "Unknown";
        }

        // Not implemented because conversion back (string → int) is not required
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
