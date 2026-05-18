using System;

namespace IPCSoftware.App.Bending.Models
{

    public class DashboardInspectionModel
    {

        public string BatchNo { get; set; }
        public DashboardInspectionLineModel LineItem1 { get; set; }
        public DashboardInspectionLineModel LineItem2 { get; set; }
        public DashboardInspectionLineModel LineItem3 { get; set; }
        public DashboardInspectionLineModel LineItem4 { get; set; }

    }
    public class DashboardInspectionLineModel
    {

        public string QRCode1 { get; set; }
        public float HeaterTemp_Bend1 { get; set; }
        public float HeaterTemp_Bend2 { get; set; }
        public float HeaterTemp_Bend3 { get; set; }

        public int Load_Bend1 { get; set; }
        public int Load_Bend2 { get; set; }
        public int Load_Bend3 { get; set; }

        public float XValue { get; set; }
        public float YValue { get; set; }
        public float ZValue { get; set; }
        public float WValue { get; set; }

        public bool Result1 { get; set; }
    }

    public class BendingIndicator
    {
        public bool Clamp { get; set; }
        public bool Heat { get; set; }
        public bool Punch { get; set; }
        public bool Tearing { get; set; }
        public bool Flipping { get; set; }
        public float Temperature { get; set; }
        public float Force { get; set; }

    }
    public class FlexBendingIndicator
    {
        public BendingIndicator Flex1 { get; set; }

        public BendingIndicator Flex2 { get; set; }

        public BendingIndicator Flex3 { get; set; }

        public BendingIndicator Flex4 { get; set; }

    }

    public class BendingIndicators
    {
        public FlexBendingIndicator Bending1 { get; set; }
        public FlexBendingIndicator Bending2 { get; set; }
        public FlexBendingIndicator Bending3 { get; set; }

    }

    //TurnTable1
    public class TurnTable1DataPointModel
    {
        public TurnTablePositionItemsModel Position1 { get; set; }
        public TurnTablePositionItemsModel Position2 { get; set; }
        public TurnTablePositionItemsModel Position3 { get; set; }
        public TurnTablePositionItemsModel Position4 { get; set; }

        public bool Rotate { get; set; }
    }

    public class TurnTablePositionItemsModel
    {
        public bool Flex1 { get; set; }
        public bool Flex2 { get; set; }
        public bool Flex3 { get; set; }
        public bool Flex4 { get; set; }
    }

    //TurnTable2
    public class TurnTable2DataPointModel
    {
        public TurnTable2PositionItemsModel Position1 { get; set; }


        public TurnTable2PositionItemsModel Position2 { get; set; }


        public TurnTable2PositionItemsModel Position3 { get; set; }

        public bool Rotate { get; set; }

    }

    public class TurnTable2PositionItemsModel
    {

        public bool Flex1 { get; set; }
        public bool Flex2 { get; set; }
        public bool Flex3 { get; set; }
        public bool Flex4 { get; set; }

    }


    //Transfer Module
    public class TransferModuleModel
    {
        public bool Flex1 { get; set; }
        public bool Flex2 { get; set; }
        public bool Flex3 { get; set; }
        public bool Flex4 { get; set; }
        public bool Rotate { get; set; }

    }

    //Inspection unit

    public class InspectionDataModel
    {
        public string QRCode { get; set; }
        public string Camera1 { get; set; }
        public string Camera2 { get; set; }
    }

    //Robot
    public class RobotProcessStatus
    {
        public int Movement { get; set; }
        public int Rotation { get; set; }
        public bool Rotate { get; set; }
    }

    //InputTray

    public class InputTrayModel
    {
        public bool IsLoaded { get; set; }
        public int Numberofcomponent { get; set; }
        public int TraySize { get; set; }
        public int NumberOfTrays { get; set; }
    }


    //OutputTray

    public class OutputTrayModel
    {
        public bool IsUnLoaded { get; set; }
        public int Numberofcomponent { get; set; }
        public int TraySize { get; set; }
        public int NumberOfTrays { get; set; }

    }


    //NGBIN
    public class NGBinModel
    {
        public int RejectedCount { get; set; }

        public bool IsFull { get; set; }
    }

    public class NGBinGroupModel
    {
        public NGBinModel NGBin1 { get; set; }

        public NGBinModel NGBin2 { get; set; }
    }

    // Efficiency Values

    public class EfficiencyBreakdown
    {
        public int Availability { get; set; }

        public int Performance { get; set; }

        public int Quality { get; set; }

        public int OEEDetails { get; set; }

        // Production Count

        public int OKCount { get; set; }

        public int NGCount { get; set; }

        // Time Metrics
        public string OperatingTime { get; set; } 
        public string Downtime { get; set; }
        public string CycleTime { get; set; }
    }
}