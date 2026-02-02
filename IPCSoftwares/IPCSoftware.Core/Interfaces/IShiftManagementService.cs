using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IShiftManagementService
    {
        Task InitializeAsync();
        Task<List<ShiftConfigurationModel>> GetAllShiftsAsync();
        Task<ShiftConfigurationModel> AddShiftAsync(ShiftConfigurationModel shift);
        Task<bool> UpdateShiftAsync(ShiftConfigurationModel shift);
        Task<bool> DeleteShiftAsync(int id);
    }
}
