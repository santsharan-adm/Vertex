using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class AlgorithmType
    {
        public int Value { get; set; }
        public string DisplayName { get; set; }
        public static int AlgoNo_LinearScale { get; set; }

        public AlgorithmType(int value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public class AlgorithmModel
        {
        }
    }
}
