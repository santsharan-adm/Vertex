using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.Shared.Models
{
    public class TagMappingSettings
    {
        public SystemTags System { get; set; } = new();

        public SystemTags System2 { get; set; } = new();
        public OeeTags OEE { get; set; } = new();
        public ModeTags Modes { get; set; } = new();
        public ManualTags Manual { get; set; } = new();
        public ServoTags Servo { get; set; } = new();
        public External ExternalTags { get; set; } = new();
        public Dashboard2 Dashboard2 { get; set; } = new();
        public BendingProcessTags BendingProcess { get; set; } = new();
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
        public int MacMiniNotConnected { get; set; }
        public int NoOfStation { get; set; }
        public int HeartbeatPLC1 { get; set; }
        public int HeartbeatIPC_PLC1 { get; set; }
        public int HeartbeatPLC2 { get; set; }
        public int HeartbeatIPC_PLC2 { get; set; }
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

    public class SystemTags2
    {

        public TagPair Year { get; set; } = new();
        public TagPair Month { get; set; } = new();
        public TagPair Day { get; set; } = new();
        public TagPair Hour { get; set; } = new();
        public TagPair Minute { get; set; } = new();
        public TagPair Second { get; set; } = new();

    }

    public class External
    {
        public int CavityStatus { get; set; }
        public int DataReady { get; set; }

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
        public double IdealCycleTime { get; set; }

        // public int AckLimitWrite { get; set; }
        public TagPair AckLimit { get; set; } = new();
        public TagPair MinX { get; set; } = new();
        public TagPair MaxX { get; set; } = new();
        public TagPair MinY { get; set; } = new();
        public TagPair MaxY { get; set; } = new();
        public TagPair MinZ { get; set; } = new();
        public TagPair MaxZ { get; set; } = new();


    }

    public class ModeTags
    {
        public TagPair Auto { get; set; } = new();
        public TagPair DryRun { get; set; } = new();
        public TagPair CycleStop { get; set; } = new();
        public TagPair MassRTO { get; set; } = new();
        public TagPair WorkPayoutStart { get; set; } = new();

        public int AutoEnable { get; set; }
        public int DryRunEnable { get; set; }
        public int CycleStopEnable { get; set; }
        public int MassRTOEnable { get; set; }
        public int WorkPayoutStartEnable { get; set; }

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
        public int ServoSeqStart { get; set; }

        public int ManualB12 { get; set; }
        public int ManualB16 { get; set; }


        public XYPair JogSpeedLow { get; set; } = new();
        // public XYPair JogSpeedHigh{ get; set; } = new();
        public XYPair OriginOffSet { get; set; } = new();
        public XYPair MoveSpeed { get; set; } = new();
        public XYPair Accel { get; set; } = new();
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

    public class Dashboard2
    {
        public int l1_QRCode { get; set; }
        public int l2_QRCode { get; set; }

        public int l3_QRCode { get; set; }

        public int l4_QRCode { get; set; }

        public int l1_HeaterTemp_Bend1 { get; set; }
        public int l2_HeaterTemp_Bend1 { get; set; }
        public int l3_HeaterTemp_Bend1 { get; set; }
        public int l4_HeaterTemp_Bend1 { get; set; }

        public int l1_HeaterTemp_Bend2 { get; set; }
        public int l2_HeaterTemp_Bend2 { get; set; }
        public int l3_HeaterTemp_Bend2 { get; set; }
        public int l4_HeaterTemp_Bend2 { get; set; }

        public int l1_HeaterTemp_Bend3 { get; set; }
        public int l2_HeaterTemp_Bend3 { get; set; }
        public int l3_HeaterTemp_Bend3 { get; set; }
        public int l4_HeaterTemp_Bend3 { get; set; }

        public int l1_Load_Bend1 { get; set; }
        public int l2_Load_Bend1 { get; set; }
        public int l3_Load_Bend1 { get; set; }
        public int l4_Load_Bend1 { get; set; }

        public int l1_Load_Bend2 { get; set; }
        public int l2_Load_Bend2 { get; set; }
        public int l3_Load_Bend2 { get; set; }
        public int l4_Load_Bend2 { get; set; }

        public int l1_Load_Bend3 { get; set; }
        public int l2_Load_Bend3 { get; set; }
        public int l3_Load_Bend3 { get; set; }
        public int l4_Load_Bend3 { get; set; }

        public int l1_XValue { get; set; }
        public int l2_XValue { get; set; }
        public int l3_XValue { get; set; }
        public int l4_XValue { get; set; }

        public int l1_YValue { get; set; }
        public int l2_YValue { get; set; }

        public int l3_YValue { get; set; }

        public int l4_YValue { get; set; }

        public int l1_ZValue { get; set; }
        public int l2_ZValue { get; set; }
        public int l3_ZValue { get; set; }
        public int l4_ZValue { get; set; }

        public int l1_WValue { get; set; }
        public int l2_WValue { get; set; }
        public int l3_WValue { get; set; }
        public int l4_WValue { get; set; }

        public int l1_Result { get; set; }
        public int l2_Result { get; set; }
        public int l3_Result { get; set; }
        public int l4_Result { get; set; }

        public int bending1Flex1Clamp { get; set; }
        public int bending1Flex2Clamp { get; set; }
        public int bending1Flex3Clamp { get; set; }
        public int bending1Flex4Clamp { get; set; }

        public int bending1Flex1Punch { get; set; }
        public int bending1Flex2Punch { get; set; }
        public int bending1Flex3Punch { get; set; }
        public int  bending1Flex4Punch { get; set; }

        public int bending1Flex1Heat { get; set; }
        public int bending1Flex2Heat { get; set; }
        public int bending1Flex3Heat { get; set; }
        public int bending1Flex4Heat  { get; set; }

        public int bending2Flex1Clamp { get; set; }
        public int bending2Flex2Clamp { get; set; }
        public int bending2Flex3Clamp { get; set; }
        public int bending2Flex4Clamp { get; set; }

        public int bending2Flex1Punch { get; set; }
        public int bending2Flex2Punch { get; set; }
        public int bending2Flex3Punch { get; set; }
        public int bending2Flex4Punch { get; set; }

        public int bending2Flex1Heat { get; set; }
        public int bending2Flex2Heat { get; set; }
        public int bending2Flex3Heat { get; set; }
        public int bending2Flex4Heat { get; set; }

        public int bending3Flex1Clamp { get; set; }
        public int bending3Flex2Clamp { get; set; }
        public int bending3Flex3Clamp { get; set; }
        public int bending3Flex4Clamp { get; set; }

        public int bending3Flex1Punch { get; set; }
        public int bending3Flex2Punch { get; set; }
        public int bending3Flex3Punch { get; set; }
        public int bending3Flex4Punch { get; set; }

        public int bending3Flex1Heat { get; set; }
        public int bending3Flex2Heat { get; set; }
        public int bending3Flex3Heat { get; set; }
        public int bending3Flex4Heat { get; set; }

        public int tearingFlex1 { get; set; }
        public int tearingFlex2 { get; set; }
        public int tearingFlex3 { get; set; }
        public int tearingFlex4 { get; set; }

        public int flippingFlex1 { get; set; }
        public int flippingFlex2 { get; set; }
        public int flippingFlex3 { get; set; }
        public int flippingFlex4 { get; set; }

        public int l1Bending1Temperature { get; set; }
        public int l2Bending1Temperature { get; set; }
        public int l3Bending1Temperature { get; set; }
        public int l4Bending1Temperature { get; set; }

        public int l1Bending2Temperature { get; set; }
        public int l2Bending2Temperature { get; set; }
        public int l3Bending2Temperature { get; set; }
        public int l4Bending2Temperature { get; set; }

        public int l1Bending3Temperature { get; set; }
        public int l2Bending3Temperature { get; set; }
        public int l3Bending3Temperature { get; set; }
        public int l4Bending3Temperature { get; set; }

        public int l1Bending1Force { get; set; }
        public int l2Bending1Force { get; set; }
        public int l3Bending1Force { get; set; }
        public int l4Bending1Force { get; set; }

        public int l1Bending2Force { get; set; }
        public int l2Bending2Force { get; set; }
        public int l3Bending2Force { get; set; }
        public int l4Bending2Force { get; set; }

        public int l1Bending3Force { get; set; }
        public int l2Bending3Force { get; set; }
        public int l3Bending3Force { get; set; }
        public int l4Bending3Force { get; set; }
    }

    public class BendingProcessTags
    {
        // Trigger signals
        public int RobotPickDone { get; set; }
        public int TT1IndexComplete { get; set; }
        public int TransferDone { get; set; }
        public int TransferActive { get; set; }
        public int TT2Start { get; set; }
        public int Robo2Done { get; set; }
        public int CameraInspectionComplete { get; set; }
        public int InspectionStartWrite { get; set; }

        // Bending completion signals
        public int CD_B1_AllDataReadComp { get; set; }
        public int CD_B2_AllDataReadComp { get; set; }
        public int CD_B3_AllDataReadComp { get; set; }
        public int TearingComplete { get; set; }
        public int FlippingComplete { get; set; }

        // QR code tags (4 parts)
        public int QrCode1 { get; set; }
        public int QrCode2 { get; set; }
        public int QrCode3 { get; set; }
        public int QrCode4 { get; set; }

        // Bending-1 process data (4 temps, 4 loads)
        public int B1_Temp1 { get; set; }
        public int B1_Temp2 { get; set; }
        public int B1_Temp3 { get; set; }
        public int B1_Temp4 { get; set; }
        public int B1_Load1 { get; set; }
        public int B1_Load2 { get; set; }
        public int B1_Load3 { get; set; }
        public int B1_Load4 { get; set; }

        // Bending-2 process data (4 temps, 4 loads)
        public int B2_Temp1 { get; set; }
        public int B2_Temp2 { get; set; }
        public int B2_Temp3 { get; set; }
        public int B2_Temp4 { get; set; }
        public int B2_Load1 { get; set; }
        public int B2_Load2 { get; set; }
        public int B2_Load3 { get; set; }
        public int B2_Load4 { get; set; }

        // Bending-3 process data (4 temps, 4 loads, 4 X, 4 Y, 4 Z, 4 W)
        public int B3_Temp1 { get; set; }
        public int B3_Temp2 { get; set; }
        public int B3_Temp3 { get; set; }
        public int B3_Temp4 { get; set; }
        public int B3_Load1 { get; set; }
        public int B3_Load2 { get; set; }
        public int B3_Load3 { get; set; }
        public int B3_Load4 { get; set; }
        public int B3_X1 { get; set; }
        public int B3_X2 { get; set; }
        public int B3_X3 { get; set; }
        public int B3_X4 { get; set; }
        public int B3_Y1 { get; set; }
        public int B3_Y2 { get; set; }
        public int B3_Y3 { get; set; }
        public int B3_Y4 { get; set; }
        public int B3_Z1 { get; set; }
        public int B3_Z2 { get; set; }
        public int B3_Z3 { get; set; }
        public int B3_Z4 { get; set; }
        public int B3_W1 { get; set; }
        public int B3_W2 { get; set; }
        public int B3_W3 { get; set; }
        public int B3_W4 { get; set; }

        // Tearing data (4 temps)
        public int Tear_Temp1 { get; set; }
        public int Tear_Temp2 { get; set; }
        public int Tear_Temp3 { get; set; }
        public int Tear_Temp4 { get; set; }

        // Flipping data (4 forces)
        public int Flip_Force1 { get; set; }
        public int Flip_Force2 { get; set; }
        public int Flip_Force3 { get; set; }
        public int Flip_Force4 { get; set; }

        // Inspection results (4 parts)
        public int InspResult1 { get; set; }
        public int InspResult2 { get; set; }
        public int InspResult3 { get; set; }
        public int InspResult4 { get; set; }
    }

}