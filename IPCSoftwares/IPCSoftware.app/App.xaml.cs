using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using IPCSoftware.App.DI;

namespace IPCSoftware.App
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }
        public static object AuthService { get; set; }
        public static object Logger { get; set; }
        public static object CurrentUser { get; set; }
        public static string AuthDbPath { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ServiceRegistration.RegisterServices(services);

            ServiceProvider = services.BuildServiceProvider();
        }
    }

}
