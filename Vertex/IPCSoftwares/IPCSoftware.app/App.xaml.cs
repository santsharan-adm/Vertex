using IPCSoftware.App.DI;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Windows;


namespace IPCSoftware.App
{
    public partial class App : Application
    {
       // private static  Host _host;
        private IConfiguration _configuration;

        public IHost _host;

        public static ServiceProvider ServiceProvider { get; private set; }
        public static UiTcpClient TcpClient { get; private set; }

        public static event Action<ResponsePackage>? ResponseReceived;

        public static event Action? TcpReady;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ///////////////////////////////////////////
            ///

            _host = Host.CreateDefaultBuilder(e.Args)
                // You have access to hostContext here:
                .ConfigureAppConfiguration((hostContext, config) =>
                {

                    var env = hostContext.HostingEnvironment?.EnvironmentName ?? "Production";

                    // Read the environment variable (use the exact name you set)
                    var sharedConfigDir = Environment.GetEnvironmentVariable("CONFIG_DIR");

                    // Fallback to the app’s base directory if the shared dir is not available
                    var baseDir = AppContext.BaseDirectory;
                    var configDir = !string.IsNullOrWhiteSpace(sharedConfigDir) && Directory.Exists(sharedConfigDir)
                                    ? sharedConfigDir
                                    : baseDir;

                    // 🔒 Deterministic: remove defaults and set base path
                    config.Sources.Clear();
                    config.SetBasePath(configDir);

                    // Load shared JSON (fail-fast on the base file)
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

                    // Keep env vars + cmd line
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(e.Args);

                    // Log to verify where config is read from
                    System.Diagnostics.Debug.WriteLine($"[WPF] Config base path: {configDir}");
                    System.Diagnostics.Debug.WriteLine($"[WPF] Environment: {env}");


                })
                .ConfigureServices((hostContext, services) =>
                {
                    // You can use hostContext.Configuration and hostContext.HostingEnvironment here:
                    var configuration = hostContext.Configuration;
                    var env = hostContext.HostingEnvironment;


                    // services.AddSingleton<IConfiguration>(configuration);
                   // services.AddSingleton<IConfiguration>(hostContext.Configuration);
                    services.Configure<ConfigSettings>(hostContext.Configuration.GetSection("Config"));
                    services.Configure<CcdSettings>(hostContext.Configuration.GetSection("CCD"));



                    ServiceRegistration.RegisterServices(services);
                })
                .Build();

            _host.Start();

            ///////////////////////////////////////////////////////////////////////////////////////////


            //var services = new ServiceCollection();
            // ServiceRegistration.RegisterServices(services);

            //ServiceProvider = services.BuildServiceProvider();

            ServiceProvider = (ServiceProvider)_host.Services;

            TcpClient = ServiceProvider.GetService<UiTcpClient>();

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

            var tagService = ServiceProvider.GetService<IPLCTagConfigurationService>();
            if (tagService != null)
            {
                await tagService.InitializeAsync();
            }

            // TagConfigProvider.Load("Data/PLCTags.csv");




            var logConfigService = ServiceProvider.GetService<ILogConfigurationService>();
            if (logConfigService != null)
            {
                await logConfigService.InitializeAsync();
            }


            var logManagerService = ServiceProvider.GetService<ILogManagerService>();
            if (logManagerService != null)
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

            /*while (!await TcpClient.StartAsync("127.0.0.1", 5050))
            {
                await Task.Delay(2000);
            }*/
            await ConnnectUItcp();

            TcpClient.UiConnected += async connected =>
            {
                if (connected)
                    Console.WriteLine("✅  Connected");
                else
                {
                    await ConnnectUItcp();
                    Console.WriteLine("❌  Disconnected");
                }
            };



        }


        /* protected override async void OnStartup(StartupEventArgs e)
         {
             base.OnStartup(e);
             var basePath = AppDomain.CurrentDomain.BaseDirectory;

             var builder = new ConfigurationBuilder()
                 .SetBasePath(basePath)
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true);

             IConfiguration configuration = builder.Build();

 *//*            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);*//*


             var services = new ServiceCollection();
             ServiceRegistration.RegisterServices(services, configuration);

             ServiceProvider = services.BuildServiceProvider();



             TcpClient = ServiceProvider.GetService<UiTcpClient>();

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

             var tagService = ServiceProvider.GetService<IPLCTagConfigurationService>();
             if (tagService != null)
             {
                 await tagService.InitializeAsync();
             }

             // TagConfigProvider.Load("Data/PLCTags.csv");




             var logConfigService = ServiceProvider.GetService<ILogConfigurationService>();
             if (logConfigService != null)
             {
                 await logConfigService.InitializeAsync();
             }


             var logManagerService = ServiceProvider.GetService<ILogManagerService>();
             if (logManagerService != null)
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

             *//*while (!await TcpClient.StartAsync("127.0.0.1", 5050))
             {
                 await Task.Delay(2000);
             }*//*
             await ConnnectUItcp();

             TcpClient.UiConnected += async connected =>
             {
                 if (connected)
                     Console.WriteLine("✅  Connected");
                 else
                 {
                     await ConnnectUItcp();
                     Console.WriteLine("❌  Disconnected");
                 }
             };



         }

 */


        public async Task ConnnectUItcp()
        {

            while (!await TcpClient.StartAsync("127.0.0.1", 5050))
            {
                await Task.Delay(2000);
            }

        }
    }
}
