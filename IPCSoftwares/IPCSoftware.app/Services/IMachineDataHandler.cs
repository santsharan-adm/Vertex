using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPCSoftware.Shared.Models;

namespace IPCSoftware.App.Services
{
    
    /// Abstraction over ServoCalibrationViewModel's PLC write capability.
    /// Defined in Core so both App and Services can reference it without circular dependency.
 
    public interface IMachineDataHandler
    {
        //Task<Dictionary<int, bool>> WriteSelectedRecipeAsync(ServoRecipeModel recipe);
        Task<Dictionary<int, bool>> ApplyRecipeToPlcAsync(ServoRecipeModel recipe);

    }
}
