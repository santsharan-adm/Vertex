using IPCSoftware.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.NavServices
{
    public class AppInitializationService
    {
        public static async Task InitializeAllServicesAsync()
        {
            // Initialize UserManagementService FIRST (for authentication)
            var userManagementService = App.ServiceProvider.GetService<IUserManagementService>();
            if (userManagementService != null)
            {
                await userManagementService.InitializeAsync();
            }

            // Ensure default admin user exists
            var authService = App.ServiceProvider.GetService<IAuthService>();
            if (authService != null)
            {
                await authService.EnsureDefaultUserExistsAsync();
            }


            // Initialize LogConfigurationService
            var logConfigService = App.ServiceProvider.GetService<ILogConfigurationService>();
            if (logConfigService != null)
            {
                await logConfigService.InitializeAsync();
            }

            // Initialize DeviceConfigurationService
            var deviceConfigService = App.ServiceProvider.GetService<IDeviceConfigurationService>();
            if (deviceConfigService != null)
            {
                await deviceConfigService.InitializeAsync();
            }

            // Initialize AlarmConfigurationService - NEW
            var alarmConfigService = App.ServiceProvider.GetService<IAlarmConfigurationService>();
            if (alarmConfigService != null)
            {
                await alarmConfigService.InitializeAsync();
            }

            // Add other service initializations here as needed
        }
    }
}
