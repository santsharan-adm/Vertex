using IPCSoftware.App.DI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Text.Json;

namespace IPCSoftware.App
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }
        public static UiTcpClient TcpClient { get; private set; }

        public static event Action<ResponsePackage>? ResponseReceived;

        public static event Action? TcpReady;
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var services = new ServiceCollection();
            ServiceRegistration.RegisterServices(services);

            ServiceProvider = services.BuildServiceProvider();



            TcpClient = new UiTcpClient();

            TcpClient.DataReceived += (json) =>
            {
                try
                {
                    // Convert string → ResponsePackage
                    var response = JsonSerializer.Deserialize<ResponsePackage>(json);

                    if (response != null)
                    {
                        ResponseReceived?.Invoke(response);
                    }
                }
                catch (Exception ex)
                {
                    // Optional: log JSON errors
                    var logger = ServiceProvider.GetService<ILogManagerService>();

                }
            };

            await TcpClient.StartAsync("127.0.0.1", 5050);

            var logConfigService = ServiceProvider.GetService<ILogConfigurationService>();
            if (logConfigService != null )
            {
                await logConfigService.InitializeAsync();
            }


            var logManagerService= ServiceProvider.GetService<ILogManagerService>();
            if (logManagerService != null )
            {
                await logManagerService.InitializeAsync();
            }


            // Initialize UserManagementService and create default admin BEFORE showing login
            var userService = ServiceProvider.GetService<IUserManagementService>();
            if (userService != null)
            {
                await userService.InitializeAsync();
            }

            var authService = ServiceProvider.GetService<IAuthService>();
            if (authService != null)
            {
                await authService.EnsureDefaultUserExistsAsync();
            }
        }
    }
}
