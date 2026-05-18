using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IPCSoftware.App.Bending.Converters
{
    /// <summary>
    /// Converts boolean values to colors for TurnTable position indicators and Result status
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return new SolidColorBrush(Colors.Transparent);

            if (value is bool isActive)
            {
                string param = parameter?.ToString();

                if (param == "Result")
                {
                    return isActive
                        ? new SolidColorBrush(Color.FromArgb(255, 34, 197, 94))
                        : new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
                }

                if (isActive)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
                }
                else
                {
                    return new SolidColorBrush(Color.FromArgb(255, 59, 130, 246));
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
