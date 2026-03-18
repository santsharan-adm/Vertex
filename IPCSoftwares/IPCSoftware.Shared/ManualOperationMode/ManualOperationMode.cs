using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPCSoftware.Shared.Attribute;

namespace IPCSoftware.Shared.ManualOperationMode
{
    public enum ManualOperationMode
    {
        // Tray lift
        [Group("Tray Lift")] TrayLiftUp,
        [Group("Tray Lift")] TrayLiftDown,

        // Positioning Cylinder
        [Group("Positioning Cylinder")] PositioningCylinderUp,
        [Group("Positioning Cylinder")] PositioningCylinderDown,

        // Move to Position (0-12)
        [Group("Move to Position")] MoveToPos0,
        [Group("Move to Position")] MoveToPos1,
        [Group("Move to Position")] MoveToPos2,
        [Group("Move to Position")] MoveToPos3,
        [Group("Move to Position")] MoveToPos4,
        [Group("Move to Position")] MoveToPos5,
        [Group("Move to Position")] MoveToPos6,
        [Group("Move to Position")] MoveToPos7,
        [Group("Move to Position")] MoveToPos8,
        [Group("Move to Position")] MoveToPos9,
        [Group("Move to Position")] MoveToPos10,
        [Group("Move to Position")] MoveToPos11,
        [Group("Move to Position")] MoveToPos12,

        // Transport Conveyor
        [Group("Transport Conveyor")] TransportConveyorReverse,
        [Group("Transport Conveyor")] TransportConveyorForward,
        [Group("Transport Conveyor")] TransportConveyorStop,
        [Group("Transport Conveyor")] TransportConveyorLowSpeed,
        [Group("Transport Conveyor")] TransportConveyorHighSpeed,

        // Manual X-Axis
        [Group("Manual X-Axis Jog")] ManualXAxisJogBackward,
        [Group("Manual X-Axis Jog")] ManualXAxisJogForward,

        // Manual Y-Axis
        [Group("Manual Y-Axis Jog")] ManualYAxisJogBackward,
        [Group("Manual Y-Axis Jog")] ManualYAxisJogForward
    }
}
