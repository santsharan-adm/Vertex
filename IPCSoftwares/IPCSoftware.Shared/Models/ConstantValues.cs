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
       
       // public static int QR_DATA_TAG_ID = 16;
        //confirmation id=15--->true

/*        public static int TAG_QR_DATA = 16;
        public static int TAG_STATUS = 17; // 1=OK, 2=NG
        public static int TAG_X = 18;
        public static int TAG_Y = 19;
        public static int TAG_Z = 20;*/

       // public static int TAG_CTL_CYCLETIME_A1= 21;
       // public static int TAG_CycleTime= 22;
       // public static int TAG_CTL_CYCLETIME_B1= 23;

        // public static int TAG_UpTime = 24;
        // public static int TAG_DownTime = 26;

        //  public static int TAG_InFlow= 27;
        // public static int TAG_OK= 28;
        // public static int TAG_NG= 29;



        // SYSTEM & OEE (Populated from ConfigSettings.TagMapping)

        public static int TRIGGER_TAG_ID;
        public static int Return_TAG_ID;
        public static int TAG_QR_DATA;
        public static int TAG_STATUS; // 1=OK, 2=NG
        public static int TAG_X;
        public static int TAG_Y;
        public static int TAG_Z;
        public static int TAG_CTL_CYCLETIME_A1;
        public static int TAG_CycleTime;
        public static int TAG_CTL_CYCLETIME_B1;
        public static int TAG_UpTime;
        public static int TAG_DownTime;
        public static int TAG_InFlow;
        public static int TAG_OK;
        public static int TAG_NG;


        public static int TAG_Heartbeat_PLC;
        public static int TAG_Heartbeat_IPC;
        public static int TAG_TimeSync_Req;
        public static int TAG_TimeSync_Ack;
        public static int TAG_Time_Year;
        public static int TAG_Global_Ack;
        public static int TAG_Global_Reset;

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
        public static int Servo_ParamA1;
        public static int Servo_ParamA2;
        public static int Servo_ParamA3;
        public static int Servo_ParamA4;
        public static int Servo_LiveX;
        public static int Servo_LiveY;
        public static int Servo_PosX_Start;
        public static int Servo_PosY_Start;
        public static int Servo_ParamX_Start;
        public static int Servo_ParamY_Start;

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

                // System & OEE
                var sys = tags.System;
                TAG_Heartbeat_PLC = sys.HeartbeatPLC;
                TAG_Heartbeat_IPC = sys.HeartbeatIPC;
                TAG_TimeSync_Req = sys.TimeSyncReq;
                TAG_TimeSync_Ack = sys.TimeSyncAck;
                TAG_Time_Year = sys.TimeDataStart;
                TAG_Global_Ack = sys.GlobalAck;
                TAG_Global_Reset = sys.GlobalReset;

                var oee = tags.OEE;
                TRIGGER_TAG_ID = oee.TriggerCCD;
                Return_TAG_ID = oee.ReadCompleteCCD;
                TAG_QR_DATA = oee.QR2dCode;
                TAG_STATUS  = oee.Status;
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
                Servo_ParamA1 = s.ParamA1;
                Servo_ParamA2 = s.ParamA2;
                Servo_ParamA3 = s.ParamA3;
                Servo_ParamA4 = s.ParamA4;
                Servo_LiveX = s.LiveX;
                Servo_LiveY = s.LiveY;
                Servo_PosX_Start = s.PosXStart;
                Servo_PosY_Start = s.PosYStart;
                Servo_ParamX_Start = s.ParamXStart;
                Servo_ParamY_Start = s.ParamYStart;
            }
        }

    }



}
