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
    /// Converts an ON/OFF string value to a background color (Brush).
    /// "ON"  -> Light Green
    /// Others -> Light Red
    /// 
    public class OnOffToBrushConverter : IValueConverter
    {
        /// Converts the input value to a Brush based on its string content.
        /// This method is called automatically by WPF binding.
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is not null and equals "ON"

            if (value?.ToString() == "ON")

                // Return light green color when status is ON

                return new SolidColorBrush(Color.FromRgb(200, 255, 200)); // light green

            // Return light red color when status is OFF or any other value
            return new SolidColorBrush(Color.FromRgb(255, 200, 200)); // light red
        }

        /// ConvertBack is not implemented because this converter
        /// is intended for one-way binding only.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
