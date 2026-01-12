using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class TagMappingSettings
    {
        public SystemTags System { get; set; } = new();
        public OeeTags OEE { get; set; } = new();
        public ModeTags Modes { get; set; } = new();
        public ManualTags Manual { get; set; } = new();
        public ServoTags Servo { get; set; } = new();
    }

    public class TagPair
    {
        public int Write { get; set; }
        public int Read { get; set; }
    }

    public class XYPair
    {
        public int X { get; set; }
        public int Y { get; set; }
    }


    public class SystemTags
    {
        public int HeartbeatPLC { get; set; }
        public int HeartbeatIPC { get; set; }
        public int TimeSyncReq { get; set; }
        public int TimeSyncAck { get; set; }
        public int TimeDataStart { get; set; }
        public int GlobalAck { get; set; }
        public int GlobalReset { get; set; }
        public int ResetTag { get; set; }
        public int ResetAckTag { get; set; }
        public int ReverseTag { get; set; }
        public int ReverseAckTag { get; set; }

        public TagPair Year { get; set; } = new();
        public TagPair Month { get; set; } = new();
        public TagPair Day { get; set; } = new();
        public TagPair Hour { get; set; } = new();
        public TagPair Minute { get; set; } = new();
        public TagPair Second { get; set; } = new();

    }

    public class OeeTags
    {
        public int CycleStartTriggerCCD { get; set; }
        public int TriggerCCD { get; set; }
        public int ReadCompleteCCD { get; set; }
        public int Status { get; set; }
        public int ValueX { get; set; }
        public int ValueY { get; set; }
        public int ValueZ { get; set; }
        public int QR2dCode { get; set; }
        public int CtlCycleTimeA1 { get; set; }
        public int CycleTime { get; set; }
        public int CtlCycleTimeB1 { get; set; }
        public int UpTime { get; set; }
        public int DownTime { get; set; }
        public int InFlow { get; set; }
        public int OK { get; set; }
        public int NG { get; set; }
    }

    public class ModeTags
    {
        public TagPair Auto{ get; set; } = new();
        public TagPair DryRun { get; set; } = new();    
        public TagPair CycleStop { get; set; } = new();
        public TagPair MassRTO { get; set; } = new();

        public int AutoEnable { get; set; }
        public int DryRunEnable { get; set; }
        public int CycleStopEnable { get; set; }
        public int MassRTOEnable { get; set; }

    }

    public class ManualTags
    {
        public TagPair TrayLiftDown { get; set; } = new();
        public TagPair TrayLiftUp { get; set; } = new();
        public TagPair CylUp { get; set; } = new();
        public TagPair CylDown { get; set; } = new();
        public TagPair ConvFwd { get; set; } = new();
        public TagPair ConvRev { get; set; } = new();
        public TagPair ConvStop { get; set; } = new();
        public TagPair ConvLow { get; set; } = new();
        public TagPair ConvHigh { get; set; } = new();
        public TagPair XFwd { get; set; } = new();
        public TagPair XRev { get; set; } = new();
        public TagPair XLow { get; set; } = new();
        public TagPair XHigh { get; set; } = new();
        public TagPair YFwd { get; set; } = new();
        public TagPair YRev { get; set; } = new();
        public TagPair YLow { get; set; } = new();
        public TagPair YHigh { get; set; } = new();
        public TagPair PositionStart { get; set; } = new();
    }



    public class ServoTags
    {
        public int ParamA1 { get; set; }
        public int ParamA2 { get; set; }
        public int ParamA3 { get; set; }
        public int ParamA4 { get; set; }
        public int ManualB12 { get; set; }
        public int ManualB16 { get; set; }


        public XYPair JogSpeedLow { get; set; } = new();
       // public XYPair JogSpeedHigh{ get; set; } = new();
        public XYPair OriginOffSet{ get; set; } = new();
        public XYPair MoveSpeed{ get; set; } = new();
        public XYPair Accel{ get; set; } = new();
        public XYPair DeAccel { get; set; } = new();
        public XYPair PosStart { get; set; } = new();
        public XYPair Live { get; set; } = new();

        //public int LiveX { get; set; }  
       // public int LiveY { get; set; }
      //  public int PosXStart { get; set; }
       // public int PosYStart { get; set; }
       // public int ParamXStart { get; set; }
       // public int ParamYStart { get; set; }
    }
}
