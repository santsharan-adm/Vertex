using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPCSoftware.Shared.Models;

namespace IPCSoftware.Core.Interfaces
{
    
    /// Abstraction over ServoCalibrationViewModel's PLC write capability.
    /// Defined in Core so both App and Services can reference it without circular dependency.
 
    public interface IPlcRecipeWriter
    {
        Task<Dictionary<int, bool>> WriteSelectedRecipeAsync(ServoRecipeModel recipe);
    }
}
