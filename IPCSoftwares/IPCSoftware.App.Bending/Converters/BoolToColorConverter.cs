using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IPCSoftware.App.Bending.Converters
{
    /// <summary>
    /// Converts boolean values to colors for TurnTable position indicators
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                // Active position: Green with glow
                if (isActive)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)); // #10B981
                }
                // Inactive position: Blue
                else
                {
                    return new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)); // #3B82F6
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
