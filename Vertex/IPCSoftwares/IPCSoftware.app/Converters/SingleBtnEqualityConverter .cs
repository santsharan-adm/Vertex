using IPCSoftware.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IPCSoftware.App.Converters
{
    /// This converter compares two bound values and returns true if they are equal.
    /// Typically used in multi-binding scenarios (for example, to check if a button's state matches a selected value).
    public class SingleBtnEqualityConverter : IMultiValueConverter
    {

        /// Compares two values passed through MultiBinding and returns true if they are equal, false otherwise.
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if at least two values are provided
            if (values == null || values.Length < 2) return false;

            // Return true if both values are equal, otherwise false
            return Equals(values[0], values[1]);
        }

        /// ConvertBack is not implemented because reverse conversion (from single bool to multiple values)
        /// is not required for this converter.

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
