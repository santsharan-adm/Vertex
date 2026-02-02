using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IAlarmConfigurationService
    {
        Task InitializeAsync();
        Task<List<AlarmConfigurationModel>> GetAllAlarmsAsync();
        Task<AlarmConfigurationModel> GetAlarmByIdAsync(int id);
        Task<AlarmConfigurationModel> AddAlarmAsync(AlarmConfigurationModel alarm);
        Task<bool> UpdateAlarmAsync(AlarmConfigurationModel alarm);
        Task<bool> DeleteAlarmAsync(int id);
        Task<bool> AcknowledgeAlarmAsync(int id);
    }

    public interface IAlarmHistoryService
    {
        Task LogHistoryAsync(AlarmInstanceModel alarm, string user);
        Task<List<AlarmHistoryModel>> GetHistoryAsync(DateTime date);
    }

}
