using IPCSoftware.Shared;
using System;
using System.Collections.ObjectModel;

namespace IPCSoftware.Shared.Models.Bending
{

    public class DashboardInspectionModel : ObservableObjectVM
    {

        public Int32 BatchNo { get; set; }
        public Int32 PositionIndex { get; set; }
        public ObservableCollection<DashboardInspectionLineModel> LineItems { get; set; } = new ObservableCollection<DashboardInspectionLineModel>()
        {
            new DashboardInspectionLineModel()
            {
                QRCode="AAA",


            },
            new DashboardInspectionLineModel()
            {
                QRCode="BBB",
                HeaterTemp_Bend1=200

            },
           
        };
        public DashboardInspectionLineModel LineItem1 { get; set; }
        public DashboardInspectionLineModel LineItem2 { get; set; }
        public DashboardInspectionLineModel LineItem3 { get; set; }
        public DashboardInspectionLineModel LineItem4 { get; set; }

    }
    public class DashboardInspectionLineModel : ObservableObjectVM
    {
        private string _qrCode;
        public string QRCode
        {
            get => _qrCode;
            set => SetProperty(ref _qrCode, value);
        }

        private float _heaterTemp_Bend1;
        public float HeaterTemp_Bend1
        {
            get => _heaterTemp_Bend1;
            set => SetProperty(ref _heaterTemp_Bend1, value);
        }

        private float _heaterTemp_Bend2;
        public float HeaterTemp_Bend2
        {
            get => _heaterTemp_Bend2;
            set => SetProperty(ref _heaterTemp_Bend2, value);
        }

        private float _heaterTemp_Bend3;
        public float HeaterTemp_Bend3
        {
            get => _heaterTemp_Bend3;
            set => SetProperty(ref _heaterTemp_Bend3, value);
        }

        private float _load_Bend1;
        public float Load_Bend1
        {
            get => _load_Bend1;
            set => SetProperty(ref _load_Bend1, value);
        }

        private float _load_Bend2;
        public float Load_Bend2
        {
            get => _load_Bend2;
            set => SetProperty(ref _load_Bend2, value);
        }

        private float _load_Bend3;
        public float Load_Bend3
        {
            get => _load_Bend3;
            set => SetProperty(ref _load_Bend3, value);
        }

        private float _xValue;
        public float XValue
        {
            get => _xValue;
            set => SetProperty(ref _xValue, value);
        }

        private float _yValue;
        public float YValue
        {
            get => _yValue;
            set => SetProperty(ref _yValue, value);
        }

        private float _zValue;
        public float ZValue
        {
            get => _zValue;
            set => SetProperty(ref _zValue, value);
        }

        private float _wValue;
        public float WValue
        {
            get => _wValue;
            set => SetProperty(ref _wValue, value);
        }

        private bool _result1;
        public bool Result1
        {
            get => _result1;
            set => SetProperty(ref _result1, value);
        }
    }

    //public class BatchBuildQRModel : ObservableObjectVM                      // Name taken by Rishabh , and this model name will chnage when UI holds any Table Title Name accordingly- Date 29/05/26
    //{
    //    public string BatchNo { get; set; }
    //    public string QRCode1 { get; set; }
    //    public string QRCode2 { get; set; }
    //    public string QRCode3 { get; set; }
    //    public string QRCode4 { get; set; }

    //}

    public class BendingIndicator : ObservableObjectVM
    {
        public bool Clamp { get; set; }
        public bool Heat { get; set; }
        public bool Punch { get; set; }
        public bool Tearing { get; set; }
        public bool Flipping { get; set; }
        public float Temperature { get; set; }
        public float Force { get; set; }

    }
    public class FlexBendingIndicator : ObservableObjectVM
    {
        public BendingIndicator Flex1 { get; set; }

        public BendingIndicator Flex2 { get; set; }

        public BendingIndicator Flex3 { get; set; }

        public BendingIndicator Flex4 { get; set; }

    }

    public class BendingIndicators : ObservableObjectVM
    {
        public FlexBendingIndicator Bending1 { get; set; }
        public FlexBendingIndicator Bending2 { get; set; }
        public FlexBendingIndicator Bending3 { get; set; }

        public FlexBendingIndicator Tearing { get; set; }
        public FlexBendingIndicator Flipping { get; set; }

    }

    //TurnTable1
    public class TurnTable1DataPointModel : ObservableObjectVM
    {
        public TurnTablePositionItemsModel Position1 { get; set; }
        public TurnTablePositionItemsModel Position2 { get; set; }
        public TurnTablePositionItemsModel Position3 { get; set; }
        public TurnTablePositionItemsModel Position4 { get; set; }

        public bool Rotate { get; set; }
    }

    public class TurnTablePositionItemsModel : ObservableObjectVM
    {
        public bool Flex1 { get; set; }
        public bool Flex2 { get; set; }
        public bool Flex3 { get; set; }
        public bool Flex4 { get; set; }
    }

    //TurnTable2
    public class TurnTable2DataPointModel : ObservableObjectVM
    {
        public TurnTable2PositionItemsModel Position1 { get; set; }


        public TurnTable2PositionItemsModel Position2 { get; set; }


        public TurnTable2PositionItemsModel Position3 { get; set; }

        public bool Rotate { get; set; }

    }

    public class TurnTable2PositionItemsModel : ObservableObjectVM
    {

        public bool Flex1 { get; set; }
        public bool Flex2 { get; set; }
        public bool Flex3 { get; set; }
        public bool Flex4 { get; set; }

    }


    //Transfer Module
    public class TransferModuleModel : ObservableObjectVM
    {
        public bool Flex1 { get; set; }
        public bool Flex2 { get; set; }
        public bool Flex3 { get; set; }
        public bool Flex4 { get; set; }
        public bool Rotate { get; set; }

    }

    //Inspection unit

    public class InspectionDataModel : ObservableObjectVM
    {
        public string QRCode { get; set; }
        public string Cam1Imagepath { get; set; }
        public string Cam2Imagepath { get; set; }
    }

    //Robot
    public class RobotProcessStatus : ObservableObjectVM
    {
        public int Movement { get; set; }
        public int Rotation { get; set; }
        public bool Rotate { get; set; }
    }

    //InputTray

    public class InputTrayModel : ObservableObjectVM
    {
        public bool IsLoaded { get; set; }
        public int Numberofcomponent { get; set; }
        public int TraySize { get; set; }
        public int NumberOfTrays { get; set; }
    }


    //OutputTray

    public class OutputTrayModel : ObservableObjectVM
    {
        public bool IsUnLoaded { get; set; }
        public int Numberofcomponent { get; set; }
        public int TraySize { get; set; }
        public int NumberOfTrays { get; set; }

    }


    //NGBIN
    public class NGBinModel : ObservableObjectVM
    {
        public int RejectedCount { get; set; }

        public bool IsFull { get; set; }
    }

    public class NGBinGroupModel : ObservableObjectVM
    {
        public NGBinModel NGBin1 { get; set; }

        public NGBinModel NGBin2 { get; set; }
    }

    // Efficiency Values

    

    public class ControlFromService : ObservableObjectVM
    {
        public int Autorun { get; set; }

        public int DryRun { get; set; }

        public int CycleStrt { get; set; }

        public int CycleStop { get; set; }

        public int WorkPayoutStart { get; set; }

    }
}
