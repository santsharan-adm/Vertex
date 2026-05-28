using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public static class ConstantValues
    {

        // NEW: External Interface Tags
        public static int Ext_CavityStatus;
        public static int Ext_DataReady;

        public static int MACMINI_NOTCONNECTED;
        public static int NO_OF_Station;


        public static int CYCLE_START_TRIGGER_TAG_ID;
        public static int TRIGGER_TAG_ID;
        public static int Return_TAG_ID;
        public static int TAG_QR_DATA;
        public static int TAG_STATUS; // 1=OK, 2=NG
        public static int TAG_X;
        public static int TAG_Y;
        public static int TAG_Z;
        public static int TAG_W;
        public static int TAG_Heat;
        public static int TAG_Punch;
        public static int TAG_Clamp;
        public static int TAG_Tearing;
        public static int TAG_Flipping;
        public static int TAG_CTL_CYCLETIME_A1;
        public static int TAG_CycleTime;
        public static int TAG_CTL_CYCLETIME_B1;
        public static int TAG_UpTime;
        public static int TAG_DownTime;
        public static int TAG_InFlow;
        public static int TAG_OK;
        public static int TAG_NG;
        public static double IDEAL_CYCLE_TIME;
        //public static int ACK_LIMIT_WRITE;
        public static TagPair MIN_X = new();
        public static TagPair MAX_X = new();
        public static TagPair MIN_Y = new();
        public static TagPair MAX_Y = new();
        public static TagPair MIN_Z = new();
        public static TagPair MAX_Z = new();
        public static TagPair ACK_LIMIT = new();


        public static int RESET_TAG_ID; // B26 (Your Reset/Start Command)
        public static int RESET_ACK_TAG_ID;
        public static int REVERSE_TAG_ID;
        public static int REVERSE_ACK_TAG_ID;


        public static int TAG_Heartbeat_PLC;
        public static int TAG_Heartbeat_IPC;
        public static int TAG_TimeSync_Req;
        public static int TAG_TimeSync_Ack;

        public static TagPair TAG_Time_Year = new();
        public static TagPair TAG_Time_Month = new();
        public static TagPair TAG_Time_Day = new();
        public static TagPair TAG_Time_Hour = new();
        public static TagPair TAG_Time_Minute = new();
        public static TagPair TAG_Time_Second = new();


        public static int TAG_Global_Ack;
        public static int TAG_Global_Reset;

        //Modes (Write/Read Pairs)
        public static TagPair Mode_Auto = new();
        public static TagPair Mode_DryRun = new();
        public static TagPair Mode_CycleStop = new();
        public static TagPair Mode_MassRTO = new();
        public static TagPair Mode_WorkPayout = new();
        public static int Mode_Auto_Enable = new();
        public static int Mode_DryRun_Enable = new();
        public static int Mode_CycleStop_Enable = new();
        public static int Mode_MassRTO_Enable = new();
        public static int Mode_WorkPayout_Enable = new();

        // MANUAL (Write/Read Pairs)
        public static TagPair Manual_TrayDown = new();
        public static TagPair Manual_TrayUp = new();
        public static TagPair Manual_CylUp = new();
        public static TagPair Manual_CylDown = new();
        public static TagPair Manual_ConvFwd = new();
        public static TagPair Manual_ConvRev = new();
        public static TagPair Manual_ConvStop = new();
        public static TagPair Manual_ConvLow = new();
        public static TagPair Manual_ConvHigh = new();

        public static TagPair Manual_XFwd = new();
        public static TagPair Manual_XRev = new();
        public static TagPair Manual_XLow = new();
        public static TagPair Manual_XHigh = new();

        public static TagPair Manual_YFwd = new();
        public static TagPair Manual_YRev = new();
        public static TagPair Manual_YLow = new();
        public static TagPair Manual_YHigh = new();

        public static TagPair Manual_PosStart = new();

        // SERVO
        public static int Servo_ParamSave;
        public static int Servo_ParamA2;
        public static int Servo_CoordSave;
        public static int Servo_XYOrigin;
        public static int Servo_Seq_Start;
        public static int Servo_XYOriginReadX;
        public static int Servo_XYOriginReadY;

        public static XYPair Servo_JogSpeed_Low = new();
        public static XYPair Servo_OffSet = new();
        public static XYPair Servo_Move_Speed = new();
        public static XYPair Servo_Accel = new();
        public static XYPair Servo_DeAccel = new();
        public static XYPair Servo_Pos_Start = new();
        public static XYPair Servo_Live = new();

        // Dashboard2 OEE Tags
        public static string QRCode1;
        public static int L1_HeaterTemp_Bend1;
        public static int L2_HeaterTemp_Bend1;
        public static int L3_HeaterTemp_Bend1;
        public static int L4_HeaterTemp_Bend1;

        public static int HeaterTemp_Bend2;
        public static int HeaterTemp_Bend3;
        public static int Load_Bend1;
        public static int Load_Bend2;
        public static int Load_Bend3;
        public static int XValue;
        public static int YValue;
        public static int ZValue;
        public static int WValue;
        public static int Result1;

        // Bending Process Tags
        // Trigger signals
        public static int BP_RobotPickDone;
        public static int BP_TT1IndexComplete;
        public static int BP_TransferDone;
        public static int BP_TransferActive;
        public static int BP_TT2Start;
        public static int BP_Robo2Done;
        public static int BP_CameraInspectionComplete;
        public static int BP_InspectionStartWrite;

        // Bending completion signals
        public static int BP_CD_B1_AllDataReadComp;
        public static int BP_CD_B2_AllDataReadComp;
        public static int BP_CD_B3_AllDataReadComp;
        public static int BP_TearingComplete;
        public static int BP_FlippingComplete;

        // QR code tags (4 parts)
        public static int BP_QrCode1;
        public static int BP_QrCode2;
        public static int BP_QrCode3;
        public static int BP_QrCode4;

        // Bending-1 process data (4 temps, 4 loads)
        public static int BP_B1_Temp1;
        public static int BP_B1_Temp2;
        public static int BP_B1_Temp3;
        public static int BP_B1_Temp4;
        public static int BP_B1_Load1;
        public static int BP_B1_Load2;
        public static int BP_B1_Load3;
        public static int BP_B1_Load4;

        // Bending-2 process data (4 temps, 4 loads)
        public static int BP_B2_Temp1;
        public static int BP_B2_Temp2;
        public static int BP_B2_Temp3;
        public static int BP_B2_Temp4;
        public static int BP_B2_Load1;
        public static int BP_B2_Load2;
        public static int BP_B2_Load3;
        public static int BP_B2_Load4;

        // Bending-3 process data (4 temps, 4 loads, 4 X, 4 Y, 4 Z, 4 W)
        public static int BP_B3_Temp1;
        public static int BP_B3_Temp2;
        public static int BP_B3_Temp3;
        public static int BP_B3_Temp4;
        public static int BP_B3_Load1;
        public static int BP_B3_Load2;
        public static int BP_B3_Load3;
        public static int BP_B3_Load4;
        public static int BP_B3_X1;
        public static int BP_B3_X2;
        public static int BP_B3_X3;
        public static int BP_B3_X4;
        public static int BP_B3_Y1;
        public static int BP_B3_Y2;
        public static int BP_B3_Y3;
        public static int BP_B3_Y4;
        public static int BP_B3_Z1;
        public static int BP_B3_Z2;
        public static int BP_B3_Z3;
        public static int BP_B3_Z4;
        public static int BP_B3_W1;
        public static int BP_B3_W2;
        public static int BP_B3_W3;
        public static int BP_B3_W4;

        // Tearing data (4 temps)
        public static int BP_Tear_Temp1;
        public static int BP_Tear_Temp2;
        public static int BP_Tear_Temp3;
        public static int BP_Tear_Temp4;

        // Flipping data (4 forces)
        public static int BP_Flip_Force1;
        public static int BP_Flip_Force2;
        public static int BP_Flip_Force3;
        public static int BP_Flip_Force4;

        // Inspection results (4 parts)
        public static int BP_InspResult1;
        public static int BP_InspResult2;
        public static int BP_InspResult3;
        public static int BP_InspResult4;



        /// <summary>
        /// Populates static fields from the root AppConfigSettings.
        /// </summary>
        public static void Initialize(ConfigSettings rootConfig)
        {
            if (rootConfig == null) return;



            // 2. Map Tags (From Config.TagMapping)
            if (rootConfig != null && rootConfig.TagMapping != null)
            {
                var tags = rootConfig.TagMapping;

                // System 
                var sys = tags.System;
                TAG_Heartbeat_PLC = sys.HeartbeatPLC;
                MACMINI_NOTCONNECTED = sys.MacMiniNotConnected;
                NO_OF_Station = sys.NoOfStation;
                TAG_Heartbeat_IPC = sys.HeartbeatIPC;
                TAG_TimeSync_Req = sys.TimeSyncReq;
                TAG_TimeSync_Ack = sys.TimeSyncAck;

                TAG_Time_Year = sys.Year;
                TAG_Time_Month = sys.Month;
                TAG_Time_Day = sys.Day;
                TAG_Time_Hour = sys.Hour;
                TAG_Time_Minute = sys.Minute;
                TAG_Time_Second = sys.Second;

                TAG_Global_Ack = sys.GlobalAck;
                TAG_Global_Reset = sys.GlobalReset;
                RESET_TAG_ID = sys.ResetTag;
                RESET_ACK_TAG_ID = sys.ResetAckTag;
                REVERSE_TAG_ID = sys.ReverseTag;
                REVERSE_ACK_TAG_ID = sys.ReverseAckTag;

                //Oee
                var oee = tags.OEE;
                CYCLE_START_TRIGGER_TAG_ID = oee.CycleStartTriggerCCD;
                TRIGGER_TAG_ID = oee.TriggerCCD;
                Return_TAG_ID = oee.ReadCompleteCCD;
                TAG_QR_DATA = oee.QR2dCode;
                TAG_STATUS = oee.Status;
                TAG_X = oee.ValueX;
                TAG_Y = oee.ValueY;
                TAG_Z = oee.ValueZ;

                TAG_CTL_CYCLETIME_A1 = oee.CtlCycleTimeA1;
                TAG_CycleTime = oee.CycleTime;
                TAG_CTL_CYCLETIME_B1 = oee.CtlCycleTimeB1;
                TAG_UpTime = oee.UpTime;
                TAG_DownTime = oee.DownTime;
                TAG_InFlow = oee.InFlow;
                TAG_OK = oee.OK;
                TAG_NG = oee.NG;
                IDEAL_CYCLE_TIME = oee.IdealCycleTime;
                MIN_X = oee.MinX;
                MAX_X = oee.MaxX;
                MIN_Y = oee.MinY;
                MAX_Y = oee.MaxY;
                MIN_Z = oee.MinZ;
                MAX_Z = oee.MaxZ;
                // ACK_LIMIT_WRITE = oee.AckLimitWrite;
                ACK_LIMIT = oee.AckLimit;


                // Modes
                var modes = tags.Modes;
                Mode_Auto = modes.Auto;
                Mode_DryRun = modes.DryRun;
                Mode_CycleStop = modes.CycleStop;
                Mode_MassRTO = modes.MassRTO;
                Mode_WorkPayout = modes.WorkPayoutStart;

                Mode_Auto_Enable = modes.AutoEnable;
                Mode_DryRun_Enable = modes.DryRunEnable;
                Mode_CycleStop_Enable = modes.CycleStopEnable;
                Mode_MassRTO_Enable = modes.MassRTOEnable;
                Mode_WorkPayout_Enable = modes.WorkPayoutStartEnable;


                // Manual
                var m = tags.Manual;
                Manual_TrayDown = m.TrayLiftDown;
                Manual_TrayUp = m.TrayLiftUp;
                Manual_CylUp = m.CylUp;
                Manual_CylDown = m.CylDown;
                Manual_ConvFwd = m.ConvFwd;
                Manual_ConvRev = m.ConvRev;
                Manual_ConvStop = m.ConvStop;
                Manual_ConvLow = m.ConvLow;
                Manual_ConvHigh = m.ConvHigh;
                Manual_XFwd = m.XFwd;
                Manual_XRev = m.XRev;
                Manual_XLow = m.XLow;
                Manual_XHigh = m.XHigh;
                Manual_YFwd = m.YFwd;
                Manual_YRev = m.YRev;
                Manual_YLow = m.YLow;
                Manual_YHigh = m.YHigh;
                Manual_PosStart = m.PositionStart;

                // Servo
                var s = tags.Servo;
                Servo_ParamSave = s.ParamA1;
                Servo_ParamA2 = s.ParamA2;
                Servo_CoordSave = s.ParamA3;
                Servo_XYOrigin = s.ParamA4;
                Servo_Seq_Start = s.ServoSeqStart;

                Servo_XYOriginReadX = s.ManualB12;
                Servo_XYOriginReadY = s.ManualB16;

                Servo_JogSpeed_Low = s.JogSpeedLow;
                Servo_OffSet = s.OriginOffSet;
                Servo_Move_Speed = s.MoveSpeed;
                Servo_Accel = s.Accel;
                Servo_DeAccel = s.DeAccel;
                Servo_Pos_Start = s.PosStart;
                Servo_Live = s.Live;

                // External Tags
                var e = tags.ExternalTags;
                if (e != null)
                {
                    Ext_CavityStatus = e.CavityStatus;
                    Ext_DataReady = e.DataReady;
                }

                // Dashboard2 OEE Tags
                var dashboard2 = tags.Dashboard2;
                if (dashboard2 != null)
                {
                    QRCode1 = dashboard2.QRCode1;
                    L1_HeaterTemp_Bend1 = dashboard2.L1_HeaterTemp_Bend1;
                    L2_HeaterTemp_Bend1 = dashboard2.L2_HeaterTemp_Bend1;
                    L3_HeaterTemp_Bend1 = dashboard2.L3_HeaterTemp_Bend1;
                    L4_HeaterTemp_Bend1 = dashboard2.L4_HeaterTemp_Bend1;

                    HeaterTemp_Bend2 = dashboard2.HeaterTemp_Bend2;
                    HeaterTemp_Bend3 = dashboard2.HeaterTemp_Bend3;
                    Load_Bend1 = dashboard2.Load_Bend1;
                    Load_Bend2 = dashboard2.Load_Bend2;
                    Load_Bend3 = dashboard2.Load_Bend3;
                    XValue = dashboard2.XValue;
                    YValue = dashboard2.YValue;
                    ZValue = dashboard2.ZValue;
                    WValue = dashboard2.WValue;
                    Result1 = dashboard2.Result1;
                }

                // Bending Process Tags
                var bp = tags.BendingProcess;
                if (bp != null)
                {
                    // Trigger signals
                    BP_RobotPickDone = bp.RobotPickDone;
                    BP_TT1IndexComplete = bp.TT1IndexComplete;
                    BP_TransferDone = bp.TransferDone;
                    BP_TransferActive = bp.TransferActive;
                    BP_TT2Start = bp.TT2Start;
                    BP_Robo2Done = bp.Robo2Done;
                    BP_CameraInspectionComplete = bp.CameraInspectionComplete;
                    BP_InspectionStartWrite = bp.InspectionStartWrite;

                    // Bending completion signals
                    BP_CD_B1_AllDataReadComp = bp.CD_B1_AllDataReadComp;
                    BP_CD_B2_AllDataReadComp = bp.CD_B2_AllDataReadComp;
                    BP_CD_B3_AllDataReadComp = bp.CD_B3_AllDataReadComp;
                    BP_TearingComplete = bp.TearingComplete;
                    BP_FlippingComplete = bp.FlippingComplete;

                    // QR code tags
                    BP_QrCode1 = bp.QrCode1;
                    BP_QrCode2 = bp.QrCode2;
                    BP_QrCode3 = bp.QrCode3;
                    BP_QrCode4 = bp.QrCode4;

                    // Bending-1 process data
                    BP_B1_Temp1 = bp.B1_Temp1;
                    BP_B1_Temp2 = bp.B1_Temp2;
                    BP_B1_Temp3 = bp.B1_Temp3;
                    BP_B1_Temp4 = bp.B1_Temp4;
                    BP_B1_Load1 = bp.B1_Load1;
                    BP_B1_Load2 = bp.B1_Load2;
                    BP_B1_Load3 = bp.B1_Load3;
                    BP_B1_Load4 = bp.B1_Load4;

                    // Bending-2 process data
                    BP_B2_Temp1 = bp.B2_Temp1;
                    BP_B2_Temp2 = bp.B2_Temp2;
                    BP_B2_Temp3 = bp.B2_Temp3;
                    BP_B2_Temp4 = bp.B2_Temp4;
                    BP_B2_Load1 = bp.B2_Load1;
                    BP_B2_Load2 = bp.B2_Load2;
                    BP_B2_Load3 = bp.B2_Load3;
                    BP_B2_Load4 = bp.B2_Load4;

                    // Bending-3 process data
                    BP_B3_Temp1 = bp.B3_Temp1;
                    BP_B3_Temp2 = bp.B3_Temp2;
                    BP_B3_Temp3 = bp.B3_Temp3;
                    BP_B3_Temp4 = bp.B3_Temp4;
                    BP_B3_Load1 = bp.B3_Load1;
                    BP_B3_Load2 = bp.B3_Load2;
                    BP_B3_Load3 = bp.B3_Load3;
                    BP_B3_Load4 = bp.B3_Load4;
                    BP_B3_X1 = bp.B3_X1;
                    BP_B3_X2 = bp.B3_X2;
                    BP_B3_X3 = bp.B3_X3;
                    BP_B3_X4 = bp.B3_X4;
                    BP_B3_Y1 = bp.B3_Y1;
                    BP_B3_Y2 = bp.B3_Y2;
                    BP_B3_Y3 = bp.B3_Y3;
                    BP_B3_Y4 = bp.B3_Y4;
                    BP_B3_Z1 = bp.B3_Z1;
                    BP_B3_Z2 = bp.B3_Z2;
                    BP_B3_Z3 = bp.B3_Z3;
                    BP_B3_Z4 = bp.B3_Z4;
                    BP_B3_W1 = bp.B3_W1;
                    BP_B3_W2 = bp.B3_W2;
                    BP_B3_W3 = bp.B3_W3;
                    BP_B3_W4 = bp.B3_W4;

                    // Tearing data
                    BP_Tear_Temp1 = bp.Tear_Temp1;
                    BP_Tear_Temp2 = bp.Tear_Temp2;
                    BP_Tear_Temp3 = bp.Tear_Temp3;
                    BP_Tear_Temp4 = bp.Tear_Temp4;

                    // Flipping data
                    BP_Flip_Force1 = bp.Flip_Force1;
                    BP_Flip_Force2 = bp.Flip_Force2;
                    BP_Flip_Force3 = bp.Flip_Force3;
                    BP_Flip_Force4 = bp.Flip_Force4;

                    // Inspection results
                    BP_InspResult1 = bp.InspResult1;
                    BP_InspResult2 = bp.InspResult2;
                    BP_InspResult3 = bp.InspResult3;
                    BP_InspResult4 = bp.InspResult4;
                }

            }
        }
    }

}




