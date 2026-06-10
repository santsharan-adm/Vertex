using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IPCSoftware.App.Bending.Converters
{
    /// <summary>
    /// Converts a width value to (width - offset). Offset is passed via ConverterParameter.
    /// Usage in XAML (inside the control template or resources):
    /// 1) Add the converter to resources:
    ///    <conv:WidthMinusValueConverter x:Key="WidthMinus" />
    /// 2) Bind to the templated parent's ActualWidth and subtract an offset:
    ///    Width="{Binding ActualWidth,
    ///                    RelativeSource={RelativeSource TemplatedParent},
    ///                    Converter={StaticResource WidthMinus},
    ///                    ConverterParameter=40}"
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class WidthMinusValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                double offset = 0;
                if (parameter != null)
                {
                    // Allow numeric strings or doubles
                    if (parameter is double d)
                    {
                        offset = d;
                    }
                    else if (!double.TryParse(parameter.ToString(), NumberStyles.Any, culture, out d))
                    {
                        d = 0;
                    }
                    offset = d;
                }

                double result = width - offset;
                if (result < 0) result = 0;
                // If target type is Thickness/Double? Usually double
                if (targetType == typeof(double) || targetType == typeof(object))
                    return result;

                try
                {
                    return System.Convert.ChangeType(result, targetType, culture);
                }
                catch
                {
                    return result;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("WidthMinusValueConverter does not support ConvertBack.");
        }
    }
}