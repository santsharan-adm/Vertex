using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.Bending.Models
{
    public class PostBendingMonitorModel
    {
        public string BatchNo { get; set; }

        public PostBendingMonitorRow Product1 { get; set; }

        public PostBendingMonitorRow Product2 { get; set; }

        public PostBendingMonitorRow Product3 { get; set; }

        public PostBendingMonitorRow Product4 { get; set; }
    }


    public class PostBendingMonitorRow
    {
        public string Product { get; set; }


        public string QRCode { get; set; }

        //-- X --//

        public ParameterLImitValues X { get; set; }

        //-- Y --//

        public ParameterLImitValues Y { get; set; }

        //-- Z --//

        public ParameterLImitValues Z { get; set; }

        //-- W --//

        public ParameterLImitValues W { get; set; }

        //-- Result --//

        public bool Result { get; set; }
    }

    public class ParameterLImitValues
    {
        public double UpperLimit { get; set; }

        public double PresentValue { get; set; }

        public double LowerLimit { get; set; }

        // Aliases for compatibility
        public double Upper { get => UpperLimit; set => UpperLimit = value; }
        public double Value { get => PresentValue; set => PresentValue = value; }
        public double Lower { get => LowerLimit; set => LowerLimit = value; }
    }
}
