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
    /// Compares two bound values and returns true if they are equal.
    /// Commonly used for enabling, checking, or highlighting a single button
    /// based on a selected value.
    public class SingleBtnEqualityConverter : IMultiValueConverter
    {
        /// Compares two values passed from a MultiBinding.

        /// True if both values are equal; otherwise false
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Validate input array and ensure at least two values exist
            if (values == null || values.Length < 2) return false;

            // Compare the first two values for equality
            return Equals(values[0], values[1]);
        }


        /// ConvertBack is not implemented because this converter
        /// is intended for one-way MultiBinding only.
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
