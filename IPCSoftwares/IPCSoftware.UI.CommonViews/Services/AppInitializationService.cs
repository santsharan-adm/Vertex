using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace IPCSoftware.Common.CommonFunctions
{
    public class AppInitializationService
    {
        public static async Task InitializeAllServicesAsync()
        {
            // Initialize UserManagementService FIRST (for authentication)
            var userManagementService = ServiceLocator.GetService<IUserManagementService>();
            if (userManagementService != null)
            {
                await userManagementService.InitializeAsync();
            }

            // Ensure default admin user exists
            var authService = ServiceLocator.GetService<IAuthService>();
            if (authService != null)
            {
                await authService.EnsureDefaultUserExistsAsync();
            }

            // Initialize LogConfigurationService
            var logConfigService = ServiceLocator.GetService<ILogConfigurationService>();
            if (logConfigService != null)
            {
                await logConfigService.InitializeAsync();
            }

            // Initialize DeviceConfigurationService
            var deviceConfigService = ServiceLocator.GetService<IDeviceConfigurationService>();
            if (deviceConfigService != null)
            {
                await deviceConfigService.InitializeAsync();
            }

            // Initialize AlarmConfigurationService
            var alarmConfigService = ServiceLocator.GetService<IAlarmConfigurationService>();
            if (alarmConfigService != null)
            {
                await alarmConfigService.InitializeAsync();
            }
            //
            //===  Added by Rishabh -Date-28-04-2026===//
            var logManagerService = ServiceLocator.GetService<ILogManagerService>();
            if (logManagerService != null)
            {
                await logManagerService.InitializeAsync();
            }

            var shiftService = ServiceLocator.GetService<IShiftManagementService>();
            if (shiftService != null)
            {
                await shiftService.InitializeAsync();
            }

            //var plcTagConfigService = ServiceLocator.GetService<IPLCTagConfigurationService>();
            //if (plcTagConfigService != null)
            //{
            //    plcTagConfigService.InitializeAsync();
            //}
        }
    }
}
