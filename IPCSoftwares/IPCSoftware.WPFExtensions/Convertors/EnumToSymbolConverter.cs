using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IPCSoftware.Common.WPFExtensions.Convertors
{
    public class EnumToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";

            var mode = (ManualOperationMode)value;
            string modeString = mode.ToString();

            // Dynamic handling for "MoveToPosN"
            if (modeString.StartsWith("MoveToPos"))
            {
                if (mode == ManualOperationMode.MoveToPos0) return "HOME";

                // Extract number: "MoveToPos12" -> "12"
                var match = Regex.Match(modeString, @"\d+");
                if (match.Success) return match.Value;
            }

            return mode switch
            {
                // Tray Lift
                ManualOperationMode.TrayLiftUp => "▲ UP",
                ManualOperationMode.TrayLiftDown => "▼ DOWN",

                // Cylinder
                ManualOperationMode.PositioningCylinderUp => "▲ UP",
                ManualOperationMode.PositioningCylinderDown => "▼ DOWN",

                // Conveyor
                ManualOperationMode.TransportConveyorReverse => "◀ REV",
                ManualOperationMode.TransportConveyorForward => "▶ FWD",
                ManualOperationMode.TransportConveyorStop => "■ STOP",
                ManualOperationMode.TransportConveyorLowSpeed => " ⏪ LOW",
                ManualOperationMode.TransportConveyorHighSpeed => "⏩ HIGH",

                // X-Axis
                ManualOperationMode.ManualXAxisJogBackward => "◀ JOG -",
                ManualOperationMode.ManualXAxisJogForward => "▶ JOG +",


                // Y-Axis
                ManualOperationMode.ManualYAxisJogBackward => "◀ JOG -",
                ManualOperationMode.ManualYAxisJogForward => "▶ JOG +",

                _ => modeString.ToUpper()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
