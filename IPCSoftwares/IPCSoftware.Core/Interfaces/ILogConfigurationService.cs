using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface ILogConfigurationService
    {
        Task<List<LogConfigurationModel>> GetAllAsync();
        Task<LogConfigurationModel> GetByIdAsync(int id);
        Task<LogConfigurationModel> AddAsync(LogConfigurationModel logConfig);
        Task<bool> UpdateAsync(LogConfigurationModel logConfig);
        Task<bool> DeleteAsync(int id);
        Task<bool> SaveChangesAsync(List<LogConfigurationModel> configurations);
    }
}
