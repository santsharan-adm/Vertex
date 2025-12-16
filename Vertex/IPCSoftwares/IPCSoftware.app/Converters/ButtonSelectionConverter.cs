using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.Converters
{
    // ButtonSelectionConverter.cs
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Data;

    namespace IPCSoftware.App.Converters
    {
        public class ButtonSelectionConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                // values[0] = SelectedGroupButtons (Dictionary<string, ManualOperationMode?>)
                // values[1] = Group (string)
                // values[2] = Mode (ManualOperationMode)

                if (values.Length < 3)
                    return false;

                var selectedGroupButtons = values[0] as Dictionary<string, object>;
                var group = values[1] as string;
                var mode = values[2];

                if (selectedGroupButtons == null || string.IsNullOrEmpty(group) || mode == null)
                    return false;

                // Check if this group has a selected button
                if (!selectedGroupButtons.ContainsKey(group))
                    return false;

                var selectedMode = selectedGroupButtons[group];

                // Return true if the selected mode matches this button's mode
                return selectedMode != null && selectedMode.Equals(mode);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

}
