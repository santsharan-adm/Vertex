using IPCSoftware.App.Bending.DI;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace IPCSoftware.App.Bending
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }
        private IHost _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Build configuration
            IConfiguration earlyConfig = new ConfigurationBuilder()
                .SetBasePath(GetConfigDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(e.Args)
                .Build();

            var configSettings = new ConfigSettings();
            earlyConfig.GetSection("Config").Bind(configSettings);
            ConstantValues.Initialize(configSettings);

            // Build host with DI
            _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    var env = hostContext.HostingEnvironment?.EnvironmentName ?? "Production";
                    var configDir = GetConfigDirectory();

                    config.Sources.Clear();
                    config.SetBasePath(configDir);

                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(e.Args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ConfigSettings>(hostContext.Configuration.GetSection("Config"));
                    services.Configure<CcdSettings>(hostContext.Configuration.GetSection("CCD"));
                    services.Configure<ExternalSettings>(hostContext.Configuration.GetSection("External"));
                    services.Configure<AboutSettings>(hostContext.Configuration.GetSection("About"));

                    ServiceRegistration.RegisterServices(services);
                })
                .Build();

            _host.Start();
            ServiceProvider = (ServiceProvider)_host.Services;

            // Initialize ServiceLocator for library projects
            ServiceLocator.Initialize(ServiceProvider);
        }

        private string GetConfigDirectory()
        {
            // Try to find config in standard locations
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var configDir = Path.Combine(baseDir, "Config");

            if (Directory.Exists(configDir))
                return configDir;

            // Fallback to base directory
            return baseDir;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }

}
