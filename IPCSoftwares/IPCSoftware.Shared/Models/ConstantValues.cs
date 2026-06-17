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


        public static int TAG_Heartbeat_PLC1;
        public static int TAG_Heartbeat_IPC_PLC1;
        public static int TAG_Heartbeat_PLC2;
        public static int TAG_Heartbeat_IPC_PLC2;
        public static int TAG_TimeSync_Req;
        public static int TAG_TimeSync_Ack;

        public static TagPair TAG_Time_Year = new();
        public static TagPair TAG_Time_Month = new();
        public static TagPair TAG_Time_Day = new();
        public static TagPair TAG_Time_Hour = new();
        public static TagPair TAG_Time_Minute = new();
        public static TagPair TAG_Time_Second = new();

        //Clock for Plc 2

        public static TagPair PLC2_TAG_Time_Year = new();
        public static TagPair PLC2_TAG_Time_Month = new();
        public static TagPair PLC2_TAG_Time_Day = new();
        public static TagPair PLC2_TAG_Time_Hour = new();
        public static TagPair PLC2_TAG_Time_Minute = new();
        public static TagPair PLC2_TAG_Time_Second = new();


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
        public static int Mode_WorkPayEnable = new();

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
        public static int TR_Robot1Intake1;
        public static int TR_InputInspectionDone;
        public static int TR_Robot1Intake2;
        public static int TR_TT1Rotate;
        public static int TR_TransferUnitRun;
        public static int TR_TT2Rotate;
        public static int TR_Robot2Intake1;
        public static int TR_Robot2Intake2;
        public static int TAG_InputQRCode1;
        public static int TAG_InputQRCode2;
        public static int TAG_InputQRCode3;
        public static int TAG_InputQRCode4;
        public static int L1_QRCode;
        public static int L2_QRCode;
        public static int L3_QRCode;
        public static int L4_QRCode;

        public static int L1_HeaterTemp_Bend1;
        public static int L2_HeaterTemp_Bend1;
        public static int L3_HeaterTemp_Bend1;
        public static int L4_HeaterTemp_Bend1;

        public static int L1_HeaterTemp_Bend2;
        public static int L2_HeaterTemp_Bend2;
        public static int L3_HeaterTemp_Bend2;
        public static int L4_HeaterTemp_Bend2;

        public static int L1_HeaterTemp_Bend3;
        public static int L2_HeaterTemp_Bend3;
        public static int L3_HeaterTemp_Bend3;
        public static int L4_HeaterTemp_Bend3;


        public static int L1_Load_Bend1;
        public static int L2_Load_Bend1;
        public static int L3_Load_Bend1;
        public static int L4_Load_Bend1;

        public static int L1_Load_Bend2;
        public static int L2_Load_Bend2;
        public static int L3_Load_Bend2;
        public static int L4_Load_Bend2;

        public static int L1_Load_Bend3;
        public static int L2_Load_Bend3;
        public static int L3_Load_Bend3;
        public static int L4_Load_Bend3;

        public static int L1_XValue;
        public static int L2_XValue;
        public static int L3_XValue;
        public static int L4_XValue;

        public static int L1_YValue;
        public static int L2_YValue;
        public static int L3_YValue;
        public static int L4_YValue;

        public static int L1_ZValue;
        public static int L2_ZValue;
        public static int L3_ZValue;
        public static int L4_ZValue;

        public static int L1_WValue;
        public static int L2_WValue;
        public static int L3_WValue;
        public static int L4_WValue;

        public static int L1_Result;
        public static int L2_Result;
        public static int L3_Result;
        public static int L4_Result;

        public static int Bending1Flex1Clamp;
        public static int Bending1Flex2Clamp;
        public static int Bending1Flex3Clamp;
        public static int Bending1Flex4Clamp;

        public static int Bending1Flex1Punch;
        public static int Bending1Flex2Punch;
        public static int Bending1Flex3Punch;
        public static int Bending1Flex4Punch;

        public static int Bending1Flex1Heat;
        public static int Bending1Flex2Heat;
        public static int Bending1Flex3Heat;
        public static int Bending1Flex4Heat;

        public static int Bending2Flex1Clamp;
        public static int Bending2Flex2Clamp;
        public static int Bending2Flex3Clamp;
        public static int Bending2Flex4Clamp;

        public static int Bending2Flex1Punch;
        public static int Bending2Flex2Punch;
        public static int Bending2Flex3Punch;
        public static int Bending2Flex4Punch;

        public static int Bending2Flex1Heat;
        public static int Bending2Flex2Heat;
        public static int Bending2Flex3Heat;
        public static int Bending2Flex4Heat;

        public static int Bending3Flex1Clamp;
        public static int Bending3Flex2Clamp;
        public static int Bending3Flex3Clamp;
        public static int Bending3Flex4Clamp;

        public static int Bending3Flex1Punch;
        public static int Bending3Flex2Punch;
        public static int Bending3Flex3Punch;
        public static int Bending3Flex4Punch;

        public static int Bending3Flex1Heat;
        public static int Bending3Flex2Heat;
        public static int Bending3Flex3Heat;
        public static int Bending3Flex4Heat;

        public static int TearingFlex1;
        public static int TearingFlex2;
        public static int TearingFlex3;
        public static int TearingFlex4;

        public static int FlippingFlex1;
        public static int FlippingFlex2;
        public static int FlippingFlex3;
        public static int FlippingFlex4;

        public static int L1Bending1Temperature;
        public static int L2Bending1Temperature;
        public static int L3Bending1Temperature;
        public static int L4Bending1Temperature;

        public static int L1Bending2Temperature;
        public static int L2Bending2Temperature;
        public static int L3Bending2Temperature;
        public static int L4Bending2Temperature;

        public static int L1Bending3Temperature;
        public static int L2Bending3Temperature;
        public static int L3Bending3Temperature;
        public static int L4Bending3Temperature;

        public static int L1Bending1Force;
        public static int L2Bending1Force;
        public static int L3Bending1Force;
        public static int L4Bending1Force;

        public static int L1Bending2Force;
        public static int L2Bending2Force;
        public static int L3Bending2Force;
        public static int L4Bending2Force;

        public static int L1Bending3Force;
        public static int L2Bending3Force;
        public static int L3Bending3Force;
        public static int L4Bending3Force;

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

        // Bending1 Monitor Tags
        public static int Bending1_BatchNo;

        // Bending1 Monitor - Product 1 (Work1)
        public static int Bending1_Product1_QRCode;
        public static int Bending1_Product1_Load_Present;
        public static int Bending1_Product1_Load_Upper;
        public static int Bending1_Product1_Load_Lower;
        public static int Bending1_Product1_Temp_Present;
        public static int Bending1_Product1_Temp_Upper;
        public static int Bending1_Product1_Temp_Lower;
        public static int Bending1_Product1_BendingTime;
        public static int Bending1_Product1_Result;

        // Bending1 Monitor - Product 2 (Work2)
        public static int Bending1_Product2_QRCode;
        public static int Bending1_Product2_Load_Present;
        public static int Bending1_Product2_Load_Upper;
        public static int Bending1_Product2_Load_Lower;
        public static int Bending1_Product2_Temp_Present;
        public static int Bending1_Product2_Temp_Upper;
        public static int Bending1_Product2_Temp_Lower;
        public static int Bending1_Product2_BendingTime;
        public static int Bending1_Product2_Result;

        // Bending1 Monitor - Product 3 (Work3)
        public static int Bending1_Product3_QRCode;
        public static int Bending1_Product3_Load_Present;
        public static int Bending1_Product3_Load_Upper;
        public static int Bending1_Product3_Load_Lower;
        public static int Bending1_Product3_Temp_Present;
        public static int Bending1_Product3_Temp_Upper;
        public static int Bending1_Product3_Temp_Lower;
        public static int Bending1_Product3_BendingTime;
        public static int Bending1_Product3_Result;

        // Bending1 Monitor - Product 4 (Work4)
        public static int Bending1_Product4_QRCode;
        public static int Bending1_Product4_Load_Present;
        public static int Bending1_Product4_Load_Upper;
        public static int Bending1_Product4_Load_Lower;
        public static int Bending1_Product4_Temp_Present;
        public static int Bending1_Product4_Temp_Upper;
        public static int Bending1_Product4_Temp_Lower;
        public static int Bending1_Product4_BendingTime;
        public static int Bending1_Product4_Result;

        // Bending1 Control Signals
        public static int Bending1_DataReadCommand;
        public static int Bending1_DataReadComplete;


        // Bending2Monitor
        public static int Bending2_BatchNo;

        // Bending2 Monitor - Product 1 (Work1)
        public static int Bending2_Product1_QRCode;
        public static int Bending2_Product1_Load_Present;
        public static int Bending2_Product1_Load_Upper;
        public static int Bending2_Product1_Load_Lower;
        public static int Bending2_Product1_Temp_Present;
        public static int Bending2_Product1_Temp_Upper;
        public static int Bending2_Product1_Temp_Lower;
        public static int Bending2_Product1_BendingTime;
        public static int Bending2_Product1_Result;

        // Bending2 Monitor - Product 2 (Work2)
        public static int Bending2_Product2_QRCode;
        public static int Bending2_Product2_Load_Present;
        public static int Bending2_Product2_Load_Upper;
        public static int Bending2_Product2_Load_Lower;
        public static int Bending2_Product2_Temp_Present;
        public static int Bending2_Product2_Temp_Upper;
        public static int Bending2_Product2_Temp_Lower;
        public static int Bending2_Product2_BendingTime;
        public static int Bending2_Product2_Result;

        // Bending2 Monitor - Product 3 (Work3)
        public static int Bending2_Product3_QRCode;
        public static int Bending2_Product3_Load_Present;
        public static int Bending2_Product3_Load_Upper;
        public static int Bending2_Product3_Load_Lower;
        public static int Bending2_Product3_Temp_Present;
        public static int Bending2_Product3_Temp_Upper;
        public static int Bending2_Product3_Temp_Lower;
        public static int Bending2_Product3_BendingTime;
        public static int Bending2_Product3_Result;

        // Bending2 Monitor - Product 4 (Work4)
        public static int Bending2_Product4_QRCode;
        public static int Bending2_Product4_Load_Present;
        public static int Bending2_Product4_Load_Upper;
        public static int Bending2_Product4_Load_Lower;
        public static int Bending2_Product4_Temp_Present;
        public static int Bending2_Product4_Temp_Upper;
        public static int Bending2_Product4_Temp_Lower;
        public static int Bending2_Product4_BendingTime;
        public static int Bending2_Product4_Result;

        // Bending2 Control Signals
        public static int Bending2_DataReadCommand;
        public static int Bending2_DataReadComplete;


        // Bending3 Monitor

        public static int Bending3_BatchNo;

        // Bending3 Monitor - Product 1 (Work1)
        public static int Bending3_Product1_QRCode;
        public static int Bending3_Product1_Load_Present;
        public static int Bending3_Product1_Load_Upper;
        public static int Bending3_Product1_Load_Lower;
        public static int Bending3_Product1_Temp_Present;
        public static int Bending3_Product1_Temp_Upper;
        public static int Bending3_Product1_Temp_Lower;
        public static int Bending3_Product1_BendingTime;
        public static int Bending3_Product1_Result;

        // Bending3 Monitor - Product 2 (Work2)
        public static int Bending3_Product2_QRCode;
        public static int Bending3_Product2_Load_Present;
        public static int Bending3_Product2_Load_Upper;
        public static int Bending3_Product2_Load_Lower;
        public static int Bending3_Product2_Temp_Present;
        public static int Bending3_Product2_Temp_Upper;
        public static int Bending3_Product2_Temp_Lower;
        public static int Bending3_Product2_BendingTime;
        public static int Bending3_Product2_Result;

        // Bending3 Monitor - Product 3 (Work3)
        public static int Bending3_Product3_QRCode;
        public static int Bending3_Product3_Load_Present;
        public static int Bending3_Product3_Load_Upper;
        public static int Bending3_Product3_Load_Lower;
        public static int Bending3_Product3_Temp_Present;
        public static int Bending3_Product3_Temp_Upper;
        public static int Bending3_Product3_Temp_Lower;
        public static int Bending3_Product3_BendingTime;
        public static int Bending3_Product3_Result;

        // Bending3 Monitor - Product 4 (Work4)
        public static int Bending3_Product4_QRCode;
        public static int Bending3_Product4_Load_Present;
        public static int Bending3_Product4_Load_Upper;
        public static int Bending3_Product4_Load_Lower;
        public static int Bending3_Product4_Temp_Present;
        public static int Bending3_Product4_Temp_Upper;
        public static int Bending3_Product4_Temp_Lower;
        public static int Bending3_Product4_BendingTime;
        public static int Bending3_Product4_Result;

        // Bending3 Control Signals
        public static int Bending3_DataReadCommand;
        public static int Bending3_DataReadComplete;

        // PostBendingMonitor
        public static int PostBending_BatchNo;

        // PostBendingMonitor - Product 1
        public static int PostBending_Product1_QRCode;
        public static int PostBending_Product1_X_Present;
        public static int PostBending_Product1_X_Upper;
        public static int PostBending_Product1_X_Lower;
        public static int PostBending_Product1_Y_Present;
        public static int PostBending_Product1_Y_Upper;
        public static int PostBending_Product1_Y_Lower;
        public static int PostBending_Product1_Z_Present;
        public static int PostBending_Product1_Z_Upper;
        public static int PostBending_Product1_Z_Lower;
        public static int PostBending_Product1_W_Present;
        public static int PostBending_Product1_W_Upper;
        public static int PostBending_Product1_W_Lower;
        public static int PostBending_Product1_Result;

        // PostBendingMonitor - Product 2
        public static int PostBending_Product2_QRCode;
        public static int PostBending_Product2_X_Present;
        public static int PostBending_Product2_X_Upper;
        public static int PostBending_Product2_X_Lower;
        public static int PostBending_Product2_Y_Present;
        public static int PostBending_Product2_Y_Upper;
        public static int PostBending_Product2_Y_Lower;
        public static int PostBending_Product2_Z_Present;
        public static int PostBending_Product2_Z_Upper;
        public static int PostBending_Product2_Z_Lower;
        public static int PostBending_Product2_W_Present;
        public static int PostBending_Product2_W_Upper;
        public static int PostBending_Product2_W_Lower;
        public static int PostBending_Product2_Result;

        // PostBendingMonitor - Product 3
        public static int PostBending_Product3_QRCode;
        public static int PostBending_Product3_X_Present;
        public static int PostBending_Product3_X_Upper;
        public static int PostBending_Product3_X_Lower;
        public static int PostBending_Product3_Y_Present;
        public static int PostBending_Product3_Y_Upper;
        public static int PostBending_Product3_Y_Lower;
        public static int PostBending_Product3_Z_Present;
        public static int PostBending_Product3_Z_Upper;
        public static int PostBending_Product3_Z_Lower;
        public static int PostBending_Product3_W_Present;
        public static int PostBending_Product3_W_Upper;
        public static int PostBending_Product3_W_Lower;
        public static int PostBending_Product3_Result;

        // PostBendingMonitor - Product 4
        public static int PostBending_Product4_QRCode;
        public static int PostBending_Product4_X_Present;
        public static int PostBending_Product4_X_Upper;
        public static int PostBending_Product4_X_Lower;
        public static int PostBending_Product4_Y_Present;
        public static int PostBending_Product4_Y_Upper;
        public static int PostBending_Product4_Y_Lower;
        public static int PostBending_Product4_Z_Present;
        public static int PostBending_Product4_Z_Upper;
        public static int PostBending_Product4_Z_Lower;
        public static int PostBending_Product4_W_Present;
        public static int PostBending_Product4_W_Upper;
        public static int PostBending_Product4_W_Lower;
        public static int PostBending_Product4_Result;

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
                TAG_Heartbeat_PLC1 = sys.HeartbeatPLC1;
                TAG_Heartbeat_PLC2 = sys.HeartbeatPLC2;
                MACMINI_NOTCONNECTED = sys.MacMiniNotConnected;
                NO_OF_Station = sys.NoOfStation;
                TAG_Heartbeat_IPC_PLC1 = sys.HeartbeatIPC_PLC1;
                TAG_Heartbeat_IPC_PLC2 = sys.HeartbeatIPC_PLC2;
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

                //System2

                var sys2 = tags.System2;
                PLC2_TAG_Time_Year = sys2.Year;
                PLC2_TAG_Time_Month = sys2.Month;
                PLC2_TAG_Time_Day = sys2.Day;
                PLC2_TAG_Time_Hour = sys2.Hour;
                PLC2_TAG_Time_Minute = sys2.Minute;
                PLC2_TAG_Time_Second = sys2.Second;

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
                Mode_WorkPayEnable = modes.WorkPayoutStartEnable;


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
                    TR_Robot1Intake1 = dashboard2.TR_Robot1Intake1;
                    TR_InputInspectionDone = dashboard2.TR_InputInspectionDone;
                    TR_Robot1Intake2 = dashboard2.TR_Robot2Intake2;
                    TR_TT1Rotate = dashboard2.TR_TT1Rotate;
                    TR_TransferUnitRun = dashboard2.TR_TransferUnitRun;
                    TR_TT2Rotate = dashboard2.TR_TT2Rotate;
                    TR_Robot2Intake1 = dashboard2.TR_Robot2Intake1;
                    TR_Robot2Intake2 = dashboard2.TR_Robot2Intake2;
                    TAG_InputQRCode1 = dashboard2.TAG_InputQRCode1;
                    TAG_InputQRCode2 = dashboard2.TAG_InputQRCode2;
                    TAG_InputQRCode3 = dashboard2.TAG_InputQRCode3;
                    TAG_InputQRCode4 = dashboard2.TAG_InputQRCode4;

                    L1_QRCode = dashboard2.l1_QRCode;
                    L2_QRCode = dashboard2.l2_QRCode;
                    L3_QRCode = dashboard2.l3_QRCode;
                    L4_QRCode = dashboard2.l4_QRCode;

                    L1_HeaterTemp_Bend1 = dashboard2.l1_HeaterTemp_Bend1;
                    L2_HeaterTemp_Bend1 = dashboard2.l2_HeaterTemp_Bend1;
                    L3_HeaterTemp_Bend1 = dashboard2.l3_HeaterTemp_Bend1;
                    L4_HeaterTemp_Bend1 = dashboard2.l4_HeaterTemp_Bend1;

                    L1_HeaterTemp_Bend2 = dashboard2.l1_HeaterTemp_Bend2;
                    L2_HeaterTemp_Bend2 = dashboard2.l2_HeaterTemp_Bend2;
                    L3_HeaterTemp_Bend2 = dashboard2.l3_HeaterTemp_Bend2;
                    L4_HeaterTemp_Bend2 = dashboard2.l4_HeaterTemp_Bend2;

                    L1_HeaterTemp_Bend3 = dashboard2.l1_HeaterTemp_Bend3;
                    L2_HeaterTemp_Bend3 = dashboard2.l2_HeaterTemp_Bend3;
                    L3_HeaterTemp_Bend3 = dashboard2.l3_HeaterTemp_Bend3;
                    L4_HeaterTemp_Bend3 = dashboard2.l4_HeaterTemp_Bend3;

                    L1_Load_Bend1 = dashboard2.l1_Load_Bend1;
                    L2_Load_Bend1 = dashboard2.l2_Load_Bend1;
                    L3_Load_Bend1 = dashboard2.l3_Load_Bend1;
                    L4_Load_Bend1 = dashboard2.l4_Load_Bend1;

                    L1_Load_Bend2 = dashboard2.l1_Load_Bend2;
                    L2_Load_Bend2 = dashboard2.l2_Load_Bend2;
                    L3_Load_Bend2 = dashboard2.l3_Load_Bend2;
                    L4_Load_Bend2 = dashboard2.l4_Load_Bend2;

                    L1_Load_Bend3 = dashboard2.l1_Load_Bend3;
                    L2_Load_Bend3 = dashboard2.l2_Load_Bend3;
                    L3_Load_Bend3 = dashboard2.l3_Load_Bend3;
                    L4_Load_Bend3 = dashboard2.l4_Load_Bend3;

                    L1_XValue = dashboard2.l1_XValue;
                    L2_XValue = dashboard2.l2_XValue;
                    L3_XValue = dashboard2.l3_XValue;
                    L4_XValue = dashboard2.l4_XValue;

                    L1_YValue = dashboard2.l1_YValue;
                    L2_YValue = dashboard2.l2_YValue;
                    L3_YValue = dashboard2.l3_YValue;
                    L4_YValue = dashboard2.l4_YValue;

                    L1_ZValue = dashboard2.l1_ZValue;
                    L2_ZValue = dashboard2.l2_ZValue;
                    L3_ZValue = dashboard2.l3_ZValue;
                    L4_ZValue = dashboard2.l4_ZValue;

                    L1_WValue = dashboard2.l1_WValue;
                    L2_WValue = dashboard2.l2_WValue;
                    L3_WValue = dashboard2.l3_WValue;
                    L4_WValue = dashboard2.l4_WValue;

                    L1_Result = dashboard2.l1_Result;
                    L2_Result = dashboard2.l2_Result;
                    L3_Result = dashboard2.l3_Result;
                    L4_Result = dashboard2.l4_Result;

                    Bending1Flex1Clamp = dashboard2.bending1Flex1Clamp;
                    Bending1Flex2Clamp = dashboard2.bending1Flex2Clamp;
                    Bending1Flex3Clamp = dashboard2.bending1Flex3Clamp;
                    Bending1Flex4Clamp = dashboard2.bending1Flex4Clamp;

                    Bending1Flex1Punch = dashboard2.bending1Flex1Punch;
                    Bending1Flex2Punch = dashboard2.bending1Flex2Punch;
                    Bending1Flex3Punch = dashboard2.bending1Flex3Punch;
                    Bending1Flex4Punch = dashboard2.bending1Flex4Punch;

                    Bending1Flex1Heat = dashboard2.bending1Flex1Heat;
                    Bending1Flex2Heat = dashboard2.bending1Flex2Heat;
                    Bending1Flex3Heat = dashboard2.bending1Flex3Heat;
                    Bending1Flex4Heat = dashboard2.bending1Flex4Heat;

                    Bending2Flex1Clamp = dashboard2.bending2Flex1Clamp;
                    Bending2Flex2Clamp = dashboard2.bending2Flex2Clamp;
                    Bending2Flex3Clamp = dashboard2.bending2Flex3Clamp;
                    Bending2Flex4Clamp = dashboard2.bending2Flex4Clamp;

                    Bending2Flex1Punch = dashboard2.bending2Flex1Punch;
                    Bending2Flex2Punch = dashboard2.bending2Flex2Punch;
                    Bending2Flex3Punch = dashboard2.bending2Flex3Punch;
                    Bending2Flex4Punch = dashboard2.bending2Flex4Punch;

                    Bending2Flex1Heat = dashboard2.bending2Flex1Heat;
                    Bending2Flex2Heat = dashboard2.bending2Flex2Heat;
                    Bending2Flex3Heat = dashboard2.bending2Flex3Heat;
                    Bending2Flex4Heat = dashboard2.bending2Flex4Heat;

                    Bending3Flex1Clamp = dashboard2.bending3Flex1Clamp;
                    Bending3Flex2Clamp = dashboard2.bending3Flex2Clamp;
                    Bending3Flex3Clamp = dashboard2.bending3Flex3Clamp;
                    Bending3Flex4Clamp = dashboard2.bending3Flex4Clamp;

                    Bending3Flex1Punch = dashboard2.bending3Flex1Punch;
                    Bending3Flex2Punch = dashboard2.bending3Flex2Punch;
                    Bending3Flex3Punch = dashboard2.bending3Flex3Punch;
                    Bending3Flex4Punch = dashboard2.bending3Flex4Punch;

                    Bending3Flex1Heat = dashboard2.bending3Flex1Heat;
                    Bending3Flex2Heat = dashboard2.bending3Flex2Heat;
                    Bending3Flex3Heat = dashboard2.bending3Flex3Heat;
                    Bending3Flex4Heat = dashboard2.bending3Flex4Heat;

                    TearingFlex1 = dashboard2.tearingFlex1;
                    TearingFlex2 = dashboard2.tearingFlex2;
                    TearingFlex3 = dashboard2.tearingFlex3;
                    TearingFlex4 = dashboard2.tearingFlex4;

                    FlippingFlex1 = dashboard2.flippingFlex1;
                    FlippingFlex2 = dashboard2.flippingFlex2;
                    FlippingFlex3 = dashboard2.flippingFlex3;
                    FlippingFlex4 = dashboard2.flippingFlex4;

                    L1Bending1Temperature = dashboard2.l1Bending1Temperature;
                    L2Bending1Temperature = dashboard2.l2Bending1Temperature;
                    L3Bending1Temperature = dashboard2.l3Bending1Temperature;
                    L4Bending1Temperature = dashboard2.l4Bending1Temperature;

                    L1Bending2Temperature = dashboard2.l1Bending2Temperature;
                    L2Bending2Temperature = dashboard2.l2Bending2Temperature;
                    L3Bending2Temperature = dashboard2.l3Bending2Temperature;
                    L4Bending2Temperature = dashboard2.l4Bending2Temperature;

                    L1Bending3Temperature = dashboard2.l1Bending3Temperature;
                    L2Bending3Temperature = dashboard2.l2Bending3Temperature;
                    L3Bending3Temperature = dashboard2.l3Bending3Temperature;
                    L4Bending3Temperature = dashboard2.l4Bending3Temperature;

                    L1Bending1Force = dashboard2.l1Bending1Force;
                    L2Bending1Force = dashboard2.l2Bending1Force;
                    L3Bending1Force = dashboard2.l3Bending1Force;
                    L4Bending1Force = dashboard2.l4Bending1Force;

                    L1Bending2Force = dashboard2.l1Bending2Force;
                    L2Bending2Force = dashboard2.l2Bending2Force;
                    L3Bending2Force = dashboard2.l3Bending2Force;
                    L4Bending2Force = dashboard2.l4Bending2Force;

                    L1Bending3Force = dashboard2.l1Bending3Force;
                    L2Bending3Force = dashboard2.l2Bending3Force;
                    L3Bending3Force = dashboard2.l3Bending3Force;
                    L4Bending3Force = dashboard2.l4Bending3Force;


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

                // Bending1Monitor Tag
                var Bending1Monitor = tags.Bending1Monitor;
                if (Bending1Monitor != null)
                {
                    Bending1_BatchNo = Bending1Monitor.Bending1_BatchNo;

                    // Bending1 Monitor - Product 1 (Work1)
                    Bending1_Product1_QRCode = Bending1Monitor.Bending1_Product1_QRCode;
                    Bending1_Product1_Load_Present = Bending1Monitor.Bending1_Product1_Load_Present;
                    Bending1_Product1_Load_Upper = Bending1Monitor.Bending1_Product1_Load_Upper;
                    Bending1_Product1_Load_Lower = Bending1Monitor.Bending1_Product1_Load_Lower;
                    Bending1_Product1_Temp_Present = Bending1Monitor.Bending1_Product1_Temp_Present;
                    Bending1_Product1_Temp_Upper = Bending1Monitor.Bending1_Product1_Temp_Upper;
                    Bending1_Product1_Temp_Lower = Bending1Monitor.Bending1_Product1_Temp_Lower;
                    Bending1_Product1_BendingTime = Bending1Monitor.Bending1_Product1_BendingTime;
                    Bending1_Product1_Result = Bending1Monitor.Bending1_Product1_Result;

                    // Bending1 Monitor - Product 2 (Work2)
                    Bending1_Product2_QRCode = Bending1Monitor.Bending1_Product2_QRCode;
                    Bending1_Product2_Load_Present = Bending1Monitor.Bending1_Product2_Load_Present;
                    Bending1_Product2_Load_Upper = Bending1Monitor.Bending1_Product2_Load_Upper;
                    Bending1_Product2_Load_Lower = Bending1Monitor.Bending1_Product2_Load_Lower;
                    Bending1_Product2_Temp_Present = Bending1Monitor.Bending1_Product2_Temp_Present;
                    Bending1_Product2_Temp_Upper = Bending1Monitor.Bending1_Product2_Temp_Upper;
                    Bending1_Product2_Temp_Lower = Bending1Monitor.Bending1_Product2_Temp_Lower;
                    Bending1_Product2_BendingTime = Bending1Monitor.Bending1_Product2_BendingTime;
                    Bending1_Product2_Result = Bending1Monitor.Bending1_Product2_Result;

                    // Bending1 Monitor - Product 3 (Work3)
                    Bending1_Product3_QRCode = Bending1Monitor.Bending1_Product3_QRCode;
                    Bending1_Product3_Load_Present = Bending1Monitor.Bending1_Product3_Load_Present;
                    Bending1_Product3_Load_Upper = Bending1Monitor.Bending1_Product3_Load_Upper;
                    Bending1_Product3_Load_Lower = Bending1Monitor.Bending1_Product3_Load_Lower;
                    Bending1_Product3_Temp_Present = Bending1Monitor.Bending1_Product3_Temp_Present;
                    Bending1_Product3_Temp_Upper = Bending1Monitor.Bending1_Product3_Temp_Upper;
                    Bending1_Product3_Temp_Lower = Bending1Monitor.Bending1_Product3_Temp_Lower;
                    Bending1_Product3_BendingTime = Bending1Monitor.Bending1_Product3_BendingTime;
                    Bending1_Product3_Result = Bending1Monitor.Bending1_Product3_Result;

                    // Bending1 Monitor - Product 4 (Work4)
                    Bending1_Product4_QRCode = Bending1Monitor.Bending1_Product4_QRCode;
                    Bending1_Product4_Load_Present = Bending1Monitor.Bending1_Product4_Load_Present;
                    Bending1_Product4_Load_Upper = Bending1Monitor.Bending1_Product4_Load_Upper;
                    Bending1_Product4_Load_Lower = Bending1Monitor.Bending1_Product4_Load_Lower;
                    Bending1_Product4_Temp_Present = Bending1Monitor.Bending1_Product4_Temp_Present;
                    Bending1_Product4_Temp_Upper = Bending1Monitor.Bending1_Product4_Temp_Upper;
                    Bending1_Product4_Temp_Lower = Bending1Monitor.Bending1_Product4_Temp_Lower;
                    Bending1_Product4_BendingTime = Bending1Monitor.Bending1_Product4_BendingTime;
                    Bending1_Product4_Result = Bending1Monitor.Bending1_Product4_Result;

                    
                    
                }

                // Bending2Monitor 

                var Bending2Monitor = tags.Bending2Monitor;
                if (Bending2Monitor != null)
                {
                    Bending2_BatchNo = Bending2Monitor.Bending2_BatchNo;
                    Bending2_Product1_QRCode = Bending2Monitor.Bending2_Product1_QRCode;
                    Bending2_Product1_Load_Present = Bending2Monitor.Bending2_Product1_Load_Present;
                    Bending2_Product1_Load_Upper = Bending2Monitor.Bending2_Product1_Load_Upper;
                    Bending2_Product1_Load_Lower = Bending2Monitor.Bending2_Product1_Load_Lower;
                    Bending2_Product1_Temp_Present = Bending2Monitor.Bending2_Product1_Temp_Present;
                    Bending2_Product1_Temp_Upper = Bending2Monitor.Bending2_Product1_Temp_Upper;
                    Bending2_Product1_Temp_Lower = Bending2Monitor.Bending2_Product1_Temp_Lower;
                    Bending2_Product1_BendingTime = Bending2Monitor.Bending2_Product1_BendingTime;
                    Bending2_Product1_Result = Bending2Monitor.Bending2_Product1_Result;

                    // Bending2 Monitor - Product 2 (Work2)
                    Bending2_Product2_QRCode = Bending2Monitor.Bending2_Product2_QRCode;
                    Bending2_Product2_Load_Present = Bending2Monitor.Bending2_Product2_Load_Present;
                    Bending2_Product2_Load_Upper = Bending2Monitor.Bending2_Product2_Load_Upper;
                    Bending2_Product2_Load_Lower = Bending2Monitor.Bending2_Product2_Load_Lower;
                    Bending2_Product2_Temp_Present = Bending2Monitor.Bending2_Product2_Temp_Present;
                    Bending2_Product2_Temp_Upper = Bending2Monitor.Bending2_Product2_Temp_Upper;
                    Bending2_Product2_Temp_Lower = Bending2Monitor.Bending2_Product2_Temp_Lower;
                    Bending2_Product2_BendingTime = Bending2Monitor.Bending2_Product2_BendingTime;
                    Bending2_Product2_Result = Bending2Monitor.Bending2_Product2_Result;

                    // Bending2 Monitor - Product 3 (Work3)
                    Bending2_Product3_QRCode = Bending2Monitor.Bending2_Product3_QRCode;
                    Bending2_Product3_Load_Present = Bending2Monitor.Bending2_Product3_Load_Present;
                    Bending2_Product3_Load_Upper = Bending2Monitor.Bending2_Product3_Load_Upper;
                    Bending2_Product3_Load_Lower = Bending2Monitor.Bending2_Product3_Load_Lower;
                    Bending2_Product3_Temp_Present = Bending2Monitor.Bending2_Product3_Temp_Present;
                    Bending2_Product3_Temp_Upper = Bending2Monitor.Bending2_Product3_Temp_Upper;
                    Bending2_Product3_Temp_Lower = Bending2Monitor.Bending2_Product3_Temp_Lower;
                    Bending2_Product3_BendingTime = Bending2Monitor.Bending2_Product3_BendingTime;
                    Bending2_Product3_Result = Bending2Monitor.Bending2_Product3_Result;

                    // Bending2 Monitor - Product 4 (Work4)
                    Bending2_Product4_QRCode = Bending2Monitor.Bending2_Product4_QRCode;
                    Bending2_Product4_Load_Present = Bending2Monitor.Bending2_Product4_Load_Present;
                    Bending2_Product4_Load_Upper = Bending2Monitor.Bending2_Product4_Load_Upper;
                    Bending2_Product4_Load_Lower = Bending2Monitor.Bending2_Product4_Load_Lower;
                    Bending2_Product4_Temp_Present = Bending2Monitor.Bending2_Product4_Temp_Present;
                    Bending2_Product4_Temp_Upper = Bending2Monitor.Bending2_Product4_Temp_Upper;
                    Bending2_Product4_Temp_Lower = Bending2Monitor.Bending2_Product4_Temp_Lower;
                    Bending2_Product4_BendingTime = Bending2Monitor.Bending2_Product4_BendingTime;
                    Bending2_Product4_Result = Bending2Monitor.Bending2_Product4_Result;

                    
                }

                // Bending3Monitor

                var Bending3Monitor = tags.Bending3Monitor;
                if (Bending3Monitor != null)
                {
                    Bending3_BatchNo = Bending3Monitor.Bending3_BatchNo;
                    Bending3_Product1_QRCode = Bending3Monitor.Bending3_Product1_QRCode;
                    Bending3_Product1_Load_Present = Bending3Monitor.Bending3_Product1_Load_Present;
                    Bending3_Product1_Load_Upper = Bending3Monitor.Bending3_Product1_Load_Upper;
                    Bending3_Product1_Load_Lower = Bending3Monitor.Bending3_Product1_Load_Lower;
                    Bending3_Product1_Temp_Present = Bending3Monitor.Bending3_Product1_Temp_Present;
                    Bending3_Product1_Temp_Upper = Bending3Monitor.Bending3_Product1_Temp_Upper;
                    Bending3_Product1_Temp_Lower = Bending3Monitor.Bending3_Product1_Temp_Lower;
                    Bending3_Product1_BendingTime = Bending3Monitor.Bending3_Product1_BendingTime;
                    Bending3_Product1_Result = Bending3Monitor.Bending3_Product1_Result;

                    // Bending3 Monitor - Product 2 (Work2)
                    Bending3_Product2_QRCode = Bending3Monitor.Bending3_Product2_QRCode;
                    Bending3_Product2_Load_Present = Bending3Monitor.Bending3_Product2_Load_Present;
                    Bending3_Product2_Load_Upper = Bending3Monitor.Bending3_Product2_Load_Upper;
                    Bending3_Product2_Load_Lower = Bending3Monitor.Bending3_Product2_Load_Lower;
                    Bending3_Product2_Temp_Present = Bending3Monitor.Bending3_Product2_Temp_Present;
                    Bending3_Product2_Temp_Upper = Bending3Monitor.Bending3_Product2_Temp_Upper;
                    Bending3_Product2_Temp_Lower = Bending3Monitor.Bending3_Product2_Temp_Lower;
                    Bending3_Product2_BendingTime = Bending3Monitor.Bending3_Product2_BendingTime;
                    Bending3_Product2_Result = Bending3Monitor.Bending3_Product2_Result;

                    // Bending3 Monitor - Product 3 (Work3)
                    Bending3_Product3_QRCode = Bending3Monitor.Bending3_Product3_QRCode;
                    Bending3_Product3_Load_Present = Bending3Monitor.Bending3_Product3_Load_Present;
                    Bending3_Product3_Load_Upper = Bending3Monitor.Bending3_Product3_Load_Upper;
                    Bending3_Product3_Load_Lower = Bending3Monitor.Bending3_Product3_Load_Lower;
                    Bending3_Product3_Temp_Present = Bending3Monitor.Bending3_Product3_Temp_Present;
                    Bending3_Product3_Temp_Upper = Bending3Monitor.Bending3_Product3_Temp_Upper;
                    Bending3_Product3_Temp_Lower = Bending3Monitor.Bending3_Product3_Temp_Lower;
                    Bending3_Product3_BendingTime = Bending3Monitor.Bending3_Product3_BendingTime;
                    Bending3_Product3_Result = Bending3Monitor.Bending3_Product3_Result;

                    // Bending3 Monitor - Product 4 (Work4)
                    Bending3_Product4_QRCode = Bending3Monitor.Bending3_Product4_QRCode;
                    Bending3_Product4_Load_Present = Bending3Monitor.Bending3_Product4_Load_Present;
                    Bending3_Product4_Load_Upper = Bending3Monitor.Bending3_Product4_Load_Upper;
                    Bending3_Product4_Load_Lower = Bending3Monitor.Bending3_Product4_Load_Lower;
                    Bending3_Product4_Temp_Present = Bending3Monitor.Bending3_Product4_Temp_Present;
                    Bending3_Product4_Temp_Upper = Bending3Monitor.Bending3_Product4_Temp_Upper;
                    Bending3_Product4_Temp_Lower = Bending3Monitor.Bending3_Product4_Temp_Lower;
                    Bending3_Product4_BendingTime = Bending3Monitor.Bending3_Product4_BendingTime;
                    Bending3_Product4_Result = Bending3Monitor.Bending3_Product4_Result;


                }

                //PostBendingMonitor

                var PostBendingMonitor = tags.PostBending;
                if (PostBendingMonitor != null)
                {
                    PostBending_BatchNo = PostBendingMonitor.BatchNo;

                    // PostBendingMonitor - Product 1
                    PostBending_Product1_QRCode = PostBendingMonitor.Product1_QRCode;
                    PostBending_Product1_X_Present = PostBendingMonitor.Product1_X_Present;
                    PostBending_Product1_X_Upper = PostBendingMonitor.Product1_X_Upper;
                    PostBending_Product1_X_Lower = PostBendingMonitor.Product1_X_Lower;
                    PostBending_Product1_Y_Present = PostBendingMonitor.Product1_Y_Present;
                    PostBending_Product1_Y_Upper = PostBendingMonitor.Product1_Y_Upper;
                    PostBending_Product1_Y_Lower = PostBendingMonitor.Product1_Y_Lower;
                    PostBending_Product1_Z_Present = PostBendingMonitor.Product1_Z_Present;
                    PostBending_Product1_Z_Upper = PostBendingMonitor.Product1_Z_Upper;
                    PostBending_Product1_Z_Lower = PostBendingMonitor.Product1_Z_Lower;
                    PostBending_Product1_W_Present = PostBendingMonitor.Product1_W_Present;
                    PostBending_Product1_W_Upper = PostBendingMonitor.Product1_W_Upper;
                    PostBending_Product1_W_Lower = PostBendingMonitor.Product1_W_Lower;
                    PostBending_Product1_Result = PostBendingMonitor.Product1_Result;

                    // PostBendingMonitor - Product 2
                    PostBending_Product2_QRCode = PostBendingMonitor.Product2_QRCode;
                    PostBending_Product2_X_Present = PostBendingMonitor.Product2_X_Present;
                    PostBending_Product2_X_Upper = PostBendingMonitor.Product2_X_Upper;
                    PostBending_Product2_X_Lower = PostBendingMonitor.Product2_X_Lower;
                    PostBending_Product2_Y_Present = PostBendingMonitor.Product2_Y_Present;
                    PostBending_Product2_Y_Upper = PostBendingMonitor.Product2_Y_Upper;
                    PostBending_Product2_Y_Lower = PostBendingMonitor.Product2_Y_Lower;
                    PostBending_Product2_Z_Present = PostBendingMonitor.Product2_Z_Present;
                    PostBending_Product2_Z_Upper = PostBendingMonitor.Product2_Z_Upper;
                    PostBending_Product2_Z_Lower = PostBendingMonitor.Product2_Z_Lower;
                    PostBending_Product2_W_Present = PostBendingMonitor.Product2_W_Present;
                    PostBending_Product2_W_Upper = PostBendingMonitor.Product2_W_Upper;
                    PostBending_Product2_W_Lower = PostBendingMonitor.Product2_W_Lower;
                    PostBending_Product2_Result = PostBendingMonitor.Product2_Result;

                    // PostBendingMonitor - Product 3
                    PostBending_Product3_QRCode = PostBendingMonitor.Product3_QRCode;
                    PostBending_Product3_X_Present = PostBendingMonitor.Product3_X_Present;
                    PostBending_Product3_X_Upper = PostBendingMonitor.Product3_X_Upper;
                    PostBending_Product3_X_Lower = PostBendingMonitor.Product3_X_Lower;
                    PostBending_Product3_Y_Present = PostBendingMonitor.Product3_Y_Present;
                    PostBending_Product3_Y_Upper = PostBendingMonitor.Product3_Y_Upper;
                    PostBending_Product3_Y_Lower = PostBendingMonitor.Product3_Y_Lower;
                    PostBending_Product3_Z_Present = PostBendingMonitor.Product3_Z_Present;
                    PostBending_Product3_Z_Upper = PostBendingMonitor.Product3_Z_Upper;
                    PostBending_Product3_Z_Lower = PostBendingMonitor.Product3_Z_Lower;
                    PostBending_Product3_W_Present = PostBendingMonitor.Product3_W_Present;
                    PostBending_Product3_W_Upper = PostBendingMonitor.Product3_W_Upper;
                    PostBending_Product3_W_Lower = PostBendingMonitor.Product3_W_Lower;
                    PostBending_Product3_Result = PostBendingMonitor.Product3_Result;

                    // PostBendingMonitor - Product 4
                    PostBending_Product4_QRCode = PostBendingMonitor.Product4_QRCode;
                    PostBending_Product4_X_Present = PostBendingMonitor.Product4_X_Present;
                    PostBending_Product4_X_Upper = PostBendingMonitor.Product4_X_Upper;
                    PostBending_Product4_X_Lower = PostBendingMonitor.Product4_X_Lower;
                    PostBending_Product4_Y_Present = PostBendingMonitor.Product4_Y_Present;
                    PostBending_Product4_Y_Upper = PostBendingMonitor.Product4_Y_Upper;
                    PostBending_Product4_Y_Lower = PostBendingMonitor.Product4_Y_Lower;
                    PostBending_Product4_Z_Present = PostBendingMonitor.Product4_Z_Present;
                    PostBending_Product4_Z_Upper = PostBendingMonitor.Product4_Z_Upper;
                    PostBending_Product4_Z_Lower = PostBendingMonitor.Product4_Z_Lower;
                    PostBending_Product4_W_Present = PostBendingMonitor.Product4_W_Present;
                    PostBending_Product4_W_Upper = PostBendingMonitor.Product4_W_Upper;
                    PostBending_Product4_W_Lower = PostBendingMonitor.Product4_W_Lower;
                    PostBending_Product4_Result = PostBendingMonitor.Product4_Result;
                }
            }
        }

    }
}




