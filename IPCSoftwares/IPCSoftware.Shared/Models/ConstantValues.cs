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

        public static string QrCodeImagePath = @"C:\Projects\Restructured\Vertex\IPCSoftwares\CCD\UI";
        public const string TempImgFolder = @"C:\Projects\Restructured\Vertex\IPCSoftwares\CCD";

        public  const int TRIGGER_TAG_ID = 10;
        public  const int Return_TAG_ID = 15;
        public const int QR_DATA_TAG_ID = 16;
        //confirmation id=15--->true

        public const string BASE_OUTPUT_DIR = @"C:\Projects\Restructured\Vertex\IPCSoftwares\ProductionImages";
        public const string METADATA_STYLE = "METADATASTYLE003";

    }



}
