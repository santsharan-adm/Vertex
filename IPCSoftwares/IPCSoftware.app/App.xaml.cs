using IPCSoftware.App.DI;
using IPCSoftware.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace IPCSoftware.App
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ServiceRegistration.RegisterServices(services);

            ServiceProvider = services.BuildServiceProvider();


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
