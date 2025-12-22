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
    //public class EqualityConverter : IMultiValueConverter
    //{


    //    //public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    //{
    //    //    if (values == null || values.Length < 2) return false;
    //    //    return Equals(values[0], values[1]);
    //    //}

    //    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (values.Length < 3)
    //            return false;

    //        var dict = values[0] as Dictionary<string, ManualOperationMode?>;
    //        var group = values[1] as string;
    //        var mode = values[2];

    //        if (dict == null || group == null || mode == null)
    //            return false;

    //        if (!dict.ContainsKey(group))
    //            return false;

    //        return dict[group]?.Equals(mode) == true;
    //    }


    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}



    /// Multi-value converter that checks whether the currently selected
    /// ManualOperationMode for a given group matches the provided mode.

    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return false;

            // values[0] = SelectedGroupButtons dictionary
            // values[1] = Group name
            // values[2] = Mode enum value

            if (values[0] is not Dictionary<string, ManualOperationMode?> selectedDict)
                return false;

            if (values[1] is not string groupName)
                return false;

            if (values[2] is not ManualOperationMode mode)
                return false;

            // Check if this group has a selection and if it matches this mode
            if (selectedDict.TryGetValue(groupName, out var selectedMode))
            {
                return selectedMode.HasValue && selectedMode.Value.Equals(mode);
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
