using IPCSoftware.Shared.Models.Bending;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IProcessLogic
    {
        void Process(Dictionary<int, object> latestValues);
        IReadOnlyList<BatchModel> GetActiveBatches();
    }
}
