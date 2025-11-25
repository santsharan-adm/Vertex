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
    public class EqualityConverter : IMultiValueConverter
    {


        //public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        //{
        //    if (values == null || values.Length < 2) return false;
        //    return Equals(values[0], values[1]);
        //}

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return false;

            var dict = values[0] as Dictionary<string, ManualOperationMode?>;
            var group = values[1] as string;
            var mode = values[2];

            if (dict == null || group == null || mode == null)
                return false;

            if (!dict.ContainsKey(group))
                return false;

            return dict[group]?.Equals(mode) == true;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
