using IPCSoftware.Shared.ManualOperationMode;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace IPCSoftware.Common.WPFExtensions.Convertors
{
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return false;

            if (values[0] is not Dictionary<string, ManualOperationMode?> selectedDict)
                return false;

            if (values[1] is not string groupName)
                return false;

            if (values[2] is not ManualOperationMode mode)
                return false;

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
