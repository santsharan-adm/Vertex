using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IUserManagementService
    {
        Task InitializeAsync();
        Task<List<UserConfigurationModel>> GetAllUsersAsync();
        Task<UserConfigurationModel> GetUserByIdAsync(int id);
        Task<UserConfigurationModel> GetUserByUsernameAsync(string username);
        Task<UserConfigurationModel> AddUserAsync(UserConfigurationModel user);
        Task<bool> UpdateUserAsync(UserConfigurationModel user);
        Task<bool> DeleteUserAsync(int id);
    }
}
