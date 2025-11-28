using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CoreService
{
    public partial class CoreServiceWorker : ServiceBase
    {
        // ===========================
        // SERVICE STATE VARIABLES
        // ===========================
        private bool _running;               // Flag to control service loop
        private TcpListener _tcpServer;      // TCP server for WPF clients
        private StreamWriter _clientWriter;  // Writer to send data to WPF

        // ===========================
        // CONFIGURATION VARIABLES
        // ===========================
        private readonly string LogFile;     // Path to core service log
        private readonly string PlcHost;     // PLC server host
        private readonly int PlcPort;        // PLC server port
        private readonly int WpfPort;        // TCP port to serve WPF clients

        // ===========================
        // CONSTRUCTOR
        // ===========================
        public CoreServiceWorker()
        {
            // Load configuration from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // Use exe folder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();

            // Assign configuration values with defaults
            LogFile = config["Logging:LogFilePath"] ?? @"C:\CoreServiceLogs\core.log";
            PlcHost = config["PLC:Host"] ?? "127.0.0.1";
            PlcPort = int.Parse(config["PLC:Port"] ?? "6000");
            WpfPort = int.Parse(config["WPF:Port"] ?? "5050");
        }

        // ===========================
        // SERVICE START
        // ===========================
        protected override void OnStart(string[] args)
        {
            _running = true;
            Log.Write(LogFile, "CoreService Started");

            // Start TCP server for WPF clients in a separate task
            Task.Run(() => StartTcpServer());

            // Start the loop to read PLC data and send to WPF
            Task.Run(() => DashboardLoop());
        }

        // ===========================
        // SERVICE STOP
        // ===========================
        protected override void OnStop()
        {
            _running = false;

            try { _tcpServer?.Stop(); } catch { }

            Log.Write(LogFile, "CoreService Stopped");
        }

        // ===========================
        // TCP SERVER FOR WPF CLIENT
        // ===========================
        private void StartTcpServer()
        {
            try
            {
                _tcpServer = new TcpListener(IPAddress.Loopback, WpfPort);
                _tcpServer.Start();
                Log.Write(LogFile, $"TCP Server started on port {WpfPort}");

                while (_running)
                {
                    // Wait for WPF client to connect
                    var client = _tcpServer.AcceptTcpClient();
                    _clientWriter = new StreamWriter(client.GetStream()) { AutoFlush = true };

                    Log.Write(LogFile, "WPF client connected");

                    // Keep connection alive while client is connected
                    while (client.Connected && _running)
                        Thread.Sleep(100);

                    // Clean up when client disconnects
                    client.Close();
                    Log.Write(LogFile, "WPF client disconnected");
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogFile, "TCP Server Error: " + ex.Message);
            }
        }

        // ===========================
        // DASHBOARD LOOP
        // Reads PLC and sends to WPF periodically
        // ===========================
        private async Task DashboardLoop()
        {
            while (_running)
            {
                // Read data from PLC
                var data = ReadPLCValues();

                if (data != null)
                {
                    // Serialize data as JSON and send to WPF
                    string json = JsonSerializer.Serialize(data);
                    _clientWriter?.WriteLine(json);

                    Log.Write(LogFile, "Sent to WPF: " + json);
                }
                else
                {
                    Log.Write(LogFile, "PLC returned NULL");
                }

                await Task.Delay(1000); // Wait 1 second between reads
            }
        }

        // ===========================
        // READ PLC DATA
        // ===========================
        private DashboardData ReadPLCValues()
        {
            try
            {
                // Connect to PLC service via TCP
                using (var client = new TcpClient(PlcHost, PlcPort))
                using (var reader = new StreamReader(client.GetStream()))
                {
                    string line = reader.ReadLine();
                    Log.Write(LogFile, "Received from PLC: " + line);

                    // Parse PLC data: OPERATING=xx;DOWN=xx;CYCLE=xx
                    var parts = line.Split(';');
                    int opSec = int.Parse(parts[0].Split('=')[1]);
                    int downSec = int.Parse(parts[1].Split('=')[1]);
                    double cycle = double.Parse(parts[2].Split('=')[1]);

                    // Return dashboard object
                    return new DashboardData
                    {
                        OperatingTime = TimeSpan.FromSeconds(opSec).ToString(@"hh\:mm\:ss"),
                        Downtime = TimeSpan.FromSeconds(downSec).ToString(@"hh\:mm\:ss"),
                        AverageCycleTime = cycle
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogFile, "PLC Read Error: " + ex.Message);
                return null;
            }
        }
    }

    // ===========================
    // THREAD-SAFE LOGGING HELPER
    // ===========================
    public static class Log
    {
        private static readonly object _lock = new object();

        public static void Write(string filePath, string message)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.AppendAllText(filePath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\n");
                }
            }
            catch { }
        }
    }
}
