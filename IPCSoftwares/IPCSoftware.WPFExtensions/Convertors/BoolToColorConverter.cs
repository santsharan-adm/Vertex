using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;


namespace IPCSoftware.Common.WPFExtensions.Convertors
{
    public class BoolToColorConverter : MarkupExtension, IValueConverter
    {
        public object True { get; set; }
        public object False { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            object colorValue = flag ? True : False;

            // Convert string hex codes (e.g., "#0F9D58") to Color objects
            if (colorValue is string colorString)
            {
                return ColorConverter.ConvertFromString(colorString);
            }
            return colorValue; // Return as is if already a Color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
