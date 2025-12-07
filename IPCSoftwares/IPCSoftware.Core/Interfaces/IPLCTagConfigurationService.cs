using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;


namespace IPCSoftware.Core.Interfaces
{
    public interface IPLCTagConfigurationService
    {
        Task InitializeAsync();
        Task<List<PLCTagConfigurationModel>> GetAllTagsAsync();
        Task<PLCTagConfigurationModel> GetTagByIdAsync(int id);
        Task<PLCTagConfigurationModel> AddTagAsync(PLCTagConfigurationModel tag);
        Task<bool> UpdateTagAsync(PLCTagConfigurationModel tag);
        Task<bool> DeleteTagAsync(int id);

      // Add the method required by TagChangeWatcherService
        Task<List<PLCTagConfigurationModel>> ReloadTagsAsync();
    }
}