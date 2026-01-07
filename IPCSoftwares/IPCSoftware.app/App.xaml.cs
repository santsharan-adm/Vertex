using IPCSoftware.App.DI;
using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Windows;


namespace IPCSoftware.App
{
    public partial class App : Application
    {
        private Mutex _mutex;
        private IConfiguration _configuration;
        public IHost _host;
        public static ServiceProvider ServiceProvider { get; private set; }
        public static UiTcpClient TcpClient { get; private set; }

        public static event Action<ResponsePackage>? ResponseReceived;
        public static event Action? TcpReady;

        // CRITICAL FIX: Prevent multiple reconnection attempts
        private bool _isReconnecting = false;
        private readonly SemaphoreSlim _reconnectLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _appCts = new CancellationTokenSource();

        protected override async void OnStartup(StartupEventArgs e)
        {
            const string appName = "Global\\IPCSoftware_UI_UniqueID";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // 2. Logic to bring existing window to front (from previous step)
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        WindowHelper.BringProcessToFront(process);
                        break;
                    }
                }

                // 3. Close this duplicate instance
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);

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
                    services.Configure<ConfigSettings>(hostContext.Configuration.GetSection("Config"));
                    services.Configure<CcdSettings>(hostContext.Configuration.GetSection("CCD"));
                    ServiceRegistration.RegisterServices(services);
                })
                .Build();

            var config = _host.Services.GetRequiredService<IConfiguration>();

            // 1. Create specific settings objects
            var configSettings = new ConfigSettings();
            var ccdSettings = new CcdSettings();

            // 2. Bind the specific sections from JSON to these objects
            config.GetSection("Config").Bind(configSettings);
            config.GetSection("CCD").Bind(ccdSettings);

            // 3. Initialize Constants without needing AppConfigSettings wrapper
            ConstantValues.Initialize(configSettings);

            _host.Start();
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

            // CRITICAL FIX: Subscribe to connection events BEFORE first connection
            TcpClient.UiConnected += OnTcpConnectionChanged;

            // Initial connection
            //await ConnectUiTcpAsync();
            _ = Task.Run(async () => await ConnectUiTcpAsync());

        }


        /// <summary>
        /// FIXED: Non-blocking reconnection handler
        /// </summary>
        private void OnTcpConnectionChanged(bool connected)
        {
            if (connected)
            {
                Console.WriteLine("✅ Connected to Core Service");
                _isReconnecting = false;
            }
            else
            {
                Console.WriteLine("❌ Disconnected from Core Service");

                // CRITICAL FIX: Fire-and-forget reconnection on background thread
                // DO NOT await this - it must not block the caller
                _ = Task.Run(async () => await ReconnectAsync());
            }
        }


        /// <summary>
        /// Initial connection with proper error handling
        /// </summary>
        private async Task ConnectUiTcpAsync()
        {
            // Infinite loop (stops only when app closes)
            while (!_appCts.Token.IsCancellationRequested)
            {
                // Try to connect
                if (await TcpClient.StartAsync("127.0.0.1", 5050))
                {
                    Console.WriteLine($"✅ Connection successful");
                    return; // EXIT THE LOOP upon success
                }

                Console.WriteLine($"⚠️ Connection failed, retrying in 2s...");

                // Wait 2 seconds before trying again
                try
                {
                    await Task.Delay(2000, _appCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break; // App is closing
                }
            }
        }

        private async Task ReconnectAsync()
        {
            // CRITICAL FIX: Prevent multiple simultaneous reconnection attempts
            if (_isReconnecting)
            {
                Console.WriteLine("[Reconnect] Already reconnecting, skipping...");
                return;
            }

            // Try to acquire the lock without blocking
            if (!await _reconnectLock.WaitAsync(0))
            {
                Console.WriteLine("[Reconnect] Another reconnection in progress, skipping...");
                return;
            }

            try
            {
                _isReconnecting = true;
                Console.WriteLine("[Reconnect] Starting reconnection loop");

                int attempts = 0;

                while (_isReconnecting && !_appCts.Token.IsCancellationRequested)
                {
                    attempts++;

                    // Exponential backoff: 2s, 4s, 6s, 8s, 10s max
                    int delay = Math.Min(2000 * attempts, 10000);

                    Console.WriteLine($"[Reconnect] Attempt {attempts} in {delay}ms...");

                    await Task.Delay(delay, _appCts.Token);

                    if (await TcpClient.StartAsync("127.0.0.1", 5050))
                    {
                        Console.WriteLine($"✅ Reconnected after {attempts} attempts");
                        _isReconnecting = false;
                        return;
                    }

                    // Show status every 5 attempts
                    if (attempts % 10 == 0)
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            var dialog = ServiceProvider.GetService<IDialogService>();
                            //dialog.ShowMessage($"Still reconnecting... (attempt {attempts})");
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Reconnect] Cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Reconnect] Error: {ex.Message}");
            }
            finally
            {
                _reconnectLock.Release();
                _isReconnecting = false;
            }
        }

      
        protected override void OnExit(ExitEventArgs e)
        {
            _appCts.Cancel();
            _isReconnecting = false;
            _reconnectLock?.Dispose();
            base.OnExit(e);
        }
    }

    public static class WindowHelper
    {
        // Win32 API Constants
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        public static void BringProcessToFront(Process process)
        {
            IntPtr handle = process.MainWindowHandle;
            if (handle == IntPtr.Zero) return;

            // 1. If minimized, restore it
            if (IsIconic(handle))
            {
                ShowWindowAsync(handle, SW_RESTORE);
            }

            // 2. Bring to foreground
            SetForegroundWindow(handle);
        }
    }
}
