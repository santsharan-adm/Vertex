using IPCSoftware.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace IPCSoftware.App.Converters
{
    /// Converts an enum value to a display-friendly string by
    /// removing the enum group prefix defined using GroupAttribute.

    public class EnumToSpacedStringConverter : IValueConverter
    {
        /// Converts enum value into a readable string without group prefix.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return empty string if no value is provided

            if (value == null) return "";

            // Convert enum value to its name

            string enumName = value.ToString();

            // Get enum member information

            var memberInfo = value.GetType().GetMember(enumName)[0];

            // Try to read GroupAttribute from enum member

            var groupAttr = memberInfo.GetCustomAttributes(typeof(GroupAttribute), false)
                                      .Cast<GroupAttribute>()
                                      .FirstOrDefault();

            // If group attribute exists, remove group name from enum name

            if (groupAttr != null)
            {
                // Remove group name (spaces removed) from the start of enum name
                var groupName = groupAttr.Name.Replace(" ", "");
                if (enumName.StartsWith(groupName))
                    enumName = enumName.Substring(groupName.Length);
            }

            return enumName;
        }

        /// ConvertBack is not required as this converter is used for display only.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// Converts ManualOperationMode enum values into
    /// user-friendly symbols and text for UI buttons.

    public class EnumToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return empty string if no value

            if (value == null) return "";

            // Cast enum value

            var mode = (ManualOperationMode)value;

            // Map enum values to symbols and labels
            return mode switch
            {
                // Tray Lift
                ManualOperationMode.TrayLiftUp => "▲ UP",
                ManualOperationMode.TrayLiftDown => "▼ DOWN",

                // Positioning Cylinder
                ManualOperationMode.PositioningCylinderUp => "▲ UP",
                ManualOperationMode.PositioningCylinderDown => "▼ DOWN",

                // Transport Conveyor
                ManualOperationMode.TransportConveyorReverse => "◀ REV",
                ManualOperationMode.TransportConveyorForward => "▶ FWD",
                ManualOperationMode.TransportConveyorStop => "■ STOP",
                ManualOperationMode.TransportConveyorSpeedSwitching => "⚡ SPEED",

                // X-Axis Jog
                ManualOperationMode.ManualXAxisJogBackward => "◀ BACK",
                ManualOperationMode.ManualXAxisJogForward => "▶ FWD",
                ManualOperationMode.XAxisJogSpeedSwitching => "⚡ SPEED",

                // Y-Axis Jog
                ManualOperationMode.ManualYAxisJogForward => "▲ UP",
                ManualOperationMode.ManualYAxisJogBackward => "▼ DOWN",
                ManualOperationMode.YAxisJogSpeedSwitching => "⚡ SPEED",

                // Positions (Just numbers now)
                ManualOperationMode.MoveToPos0 => "HOM POS",
                ManualOperationMode.MoveToPos1 => "1",
                ManualOperationMode.MoveToPos2 => "2",
                ManualOperationMode.MoveToPos3 => "3",
                ManualOperationMode.MoveToPos4 => "4",
                ManualOperationMode.MoveToPos5 => "5",
                ManualOperationMode.MoveToPos6 => "6",
                ManualOperationMode.MoveToPos7 => "7",
                ManualOperationMode.MoveToPos8 => "8",
                ManualOperationMode.MoveToPos9 => "9",
                ManualOperationMode.MoveToPos10 => "10",
                ManualOperationMode.MoveToPos11 => "11",
                ManualOperationMode.MoveToPos12 => "12",

                _ => value.ToString().ToUpper()
            };
        }

        /// ConvertBack is not supported for one-way UI bindings.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// Controls visibility of UI elements based on group matching.
    /// Used to show/hide buttons depending on selected group.


    public class GroupVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts group name to Visibility
        /// </summary>
        /// <param name="value">The group name from the button (e.g., "Tray Lift")</param>
        /// <param name="targetType">Target type (Visibility)</param>
        /// <param name="parameter">The target group name to match against (e.g., "Tray Lift")</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Visible if group matches, Collapsed otherwise</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string itemGroup = value.ToString();
            string targetGroup = parameter.ToString();

            // Show button only if its group matches the target group
            return itemGroup == targetGroup ? Visibility.Visible : Visibility.Collapsed;
        }

        /// ConvertBack is not required for visibility control.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
