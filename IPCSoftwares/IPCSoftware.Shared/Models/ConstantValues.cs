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
        public static int Ext_SeqRegStart;
        public static int MACMINI_NOTCONNECTED;


        public static int CYCLE_START_TRIGGER_TAG_ID ;
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
        public static double IDEAL_CYCLE_TIME;
        public static int ACK_LIMIT_WRITE;
        public static TagPair MIN_X = new();
        public static TagPair MAX_X = new();
        public static TagPair MIN_Y = new();
        public static TagPair MAX_Y = new();
        public static TagPair MIN_Z = new();
        public static TagPair MAX_Z = new();


        public static int RESET_TAG_ID ; // B26 (Your Reset/Start Command)
        public static int RESET_ACK_TAG_ID ;
        public static int REVERSE_TAG_ID ;
        public static int REVERSE_ACK_TAG_ID;


        public static int TAG_Heartbeat_PLC;
        public static int TAG_Heartbeat_IPC;
        public static int TAG_TimeSync_Req;
        public static int TAG_TimeSync_Ack;

        public static  TagPair TAG_Time_Year = new ();
        public static TagPair TAG_Time_Month = new ();
        public static TagPair TAG_Time_Day = new ();
        public static TagPair TAG_Time_Hour = new ();
        public static TagPair TAG_Time_Minute = new ();
        public static TagPair TAG_Time_Second = new ();


        public static int TAG_Global_Ack;
        public static int TAG_Global_Reset;

        //Modes (Write/Read Pairs)
        public static TagPair Mode_Auto = new();
        public static TagPair Mode_DryRun = new();
        public static TagPair Mode_CycleStop = new();
        public static TagPair Mode_MassRTO = new();
        public static int Mode_Auto_Enable = new();
        public static int Mode_DryRun_Enable = new();
        public static int Mode_CycleStop_Enable = new();
        public static int Mode_MassRTO_Enable = new();

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
        public static int Servo_XYOriginReadX;
        public static int Servo_XYOriginReadY;

        public static XYPair Servo_JogSpeed_Low = new();
        public static XYPair Servo_OffSet = new();
        public static XYPair Servo_Move_Speed= new();
        public static XYPair Servo_Accel= new();
        public static XYPair Servo_DeAccel= new();
        public static XYPair Servo_Pos_Start= new();
        public static XYPair Servo_Live= new();
  

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
                IDEAL_CYCLE_TIME = oee.IdealCycleTime;
                MIN_X = oee.MinX;
                MAX_X = oee.MaxX;
                MIN_Y= oee.MinY;
                MAX_Y = oee.MaxY;
                MIN_Z = oee.MinZ;
                MAX_Z = oee.MaxZ;


                // Modes
                var modes = tags.Modes;
                Mode_Auto = modes.Auto;
                Mode_DryRun = modes.DryRun;
                Mode_CycleStop = modes.CycleStop;
                Mode_MassRTO = modes.MassRTO;

                Mode_Auto_Enable = modes.AutoEnable;
                Mode_DryRun_Enable = modes.DryRunEnable;
                Mode_CycleStop_Enable = modes.CycleStopEnable;
                Mode_MassRTO_Enable = modes.MassRTOEnable;


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
                Servo_CoordSave= s.ParamA3;
                Servo_XYOrigin = s.ParamA4;
                Servo_XYOriginReadX = s.ManualB12;
                Servo_XYOriginReadY = s.ManualB16;

                Servo_JogSpeed_Low = s.JogSpeedLow;
                Servo_OffSet = s.OriginOffSet;
                Servo_Move_Speed = s.MoveSpeed;
                Servo_Accel = s.Accel;
                Servo_DeAccel = s.DeAccel;
                Servo_Pos_Start = s.PosStart;
                Servo_Live = s.Live;

                var e = tags.ExternalTags;
                if (e != null)
                {
                    Ext_CavityStatus = e.CavityStatus;
                    Ext_DataReady = e.DataReady;
                  
                    Ext_SeqRegStart = e.SeqRegStart;
                }
            }
        }

    }



}
