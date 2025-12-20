using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IServoCalibrationService
    {
        Task<List<ServoPositionModel>> LoadPositionsAsync();
        Task SavePositionsAsync(List<ServoPositionModel> positions);
    }
}
