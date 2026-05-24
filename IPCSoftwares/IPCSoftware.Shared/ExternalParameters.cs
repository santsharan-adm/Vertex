using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared
{
    public class ExternalParameters
    {

        public bool IsMacMiniEnabled { get; set; }
        public string InspectionXUnit  { get; set; }
        public string InspectionYUnit  { get; set; }
        public string InspectionAngleUnit  { get; set; }
        public string Protocol   { get; set; }
        public string MacMiniIpAddress { get; set; }
        public int Port  { get; set; }
        public string EndPoint  { get; set; }
        public string PreviousMachineCode  { get; set; }
        public string AOIMachineCode  { get; set; }


    }
}
