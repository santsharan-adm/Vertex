using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public static  class ConstantValues
    {
        public const int PollingIntervalFTP = 2;
        public  const int TRIGGER_TAG_ID = 10;
        public  const int Return_TAG_ID = 15;
        public const int QR_DATA_TAG_ID = 16;
        //confirmation id=15--->true

        public const int TAG_QR_DATA = 16;
        public const int TAG_STATUS = 17; // 1=OK, 2=NG
        public const int TAG_X = 18;
        public const int TAG_Y = 19;
        public const int TAG_Z = 20;

        public const int TAG_CTL_CYCLETIME_A1= 21;
        public const int TAG_CycleTime= 22;
        public const int TAG_CTL_CYCLETIME_B1= 23;

        public const int TAG_UpTime = 24;
        public const int TAG_DownTime = 26;

        public const int TAG_InFlow= 27;
        public const int TAG_OK= 28;
        public const int TAG_NG= 29;
    }



}
