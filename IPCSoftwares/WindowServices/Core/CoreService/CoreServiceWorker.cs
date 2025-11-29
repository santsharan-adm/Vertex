using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreService
{
    public class CoreServiceWorker : BackgroundService
    {
        private readonly ILogger<CoreServiceWorker> _logger;
        private readonly IConfiguration _config;

        private TcpListener? _tcpServer;
        private StreamWriter? _clientWriter;

        // Persistent PLC connection
        private TcpClient? _plcClient;
        private StreamReader? _plcReader;
        private StreamWriter? _plcWriter;

        private readonly string _logFile;
        private readonly string _plcHost;
        private readonly int _plcPort;
        private readonly int _wpfPort;

        public CoreServiceWorker(ILogger<CoreServiceWorker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            _logFile = config["Logging:LogFilePath"] ?? @"C:\CoreServiceLogs\core.log";
            _plcHost = config["PLC:Host"] ?? "127.0.0.1";
            _plcPort = int.Parse(config["PLC:Port"] ?? "6000");
            _wpfPort = int.Parse(config["WPF:Port"] ?? "5050");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Write(_logFile, "CoreService Started");

            // Start TCP server for WPF clients
            _ = Task.Run(() => StartTcpServerAsync(stoppingToken), stoppingToken);

            // Start dashboard loop
            while (!stoppingToken.IsCancellationRequested)
            {
                var data = await ReadPLCValuesAsync(stoppingToken);
                if (data != null)
                {
                    string json = JsonSerializer.Serialize(data);
                    _clientWriter?.WriteLine(json);
                    Log.Write(_logFile, "Sent to WPF: " + json);
                }
                else
                {
                    Log.Write(_logFile, "PLC returned NULL");
                }

                await Task.Delay(1000, stoppingToken);
            }

            Log.Write(_logFile, "CoreService Stopped");
        }

        #region PLC Connection

        private async Task EnsurePlcConnectionAsync(CancellationToken stoppingToken)
        {
            if (_plcClient != null && _plcClient.Connected)
                return;

            try
            {
                _plcClient = new TcpClient();
                await _plcClient.ConnectAsync(_plcHost, _plcPort, stoppingToken);
                _plcReader = new StreamReader(_plcClient.GetStream());
                _plcWriter = new StreamWriter(_plcClient.GetStream()) { AutoFlush = true };

                Log.Write(_logFile, "Connected to PLC");
            }
            catch (Exception ex)
            {
                Log.Write(_logFile, "PLC Connection Error: " + ex.Message);
                _plcClient?.Dispose();
                _plcClient = null;
                _plcReader = null;
                _plcWriter = null;
            }
        }

        private async Task<DashboardData?> ReadPLCValuesAsync(CancellationToken stoppingToken)
        {
            await EnsurePlcConnectionAsync(stoppingToken);
            if (_plcReader == null)
                return null;

            try
            {
                string? line = await _plcReader.ReadLineAsync(stoppingToken);
                if (string.IsNullOrEmpty(line))
                    return null;

                Log.Write(_logFile, "Received from PLC: " + line);

                var parts = line.Split(';');
                int opSec = int.Parse(parts[0].Split('=')[1]);
                int downSec = int.Parse(parts[1].Split('=')[1]);
                double cycle = double.Parse(parts[2].Split('=')[1]);

                return new DashboardData
                {
                    OperatingTime = TimeSpan.FromSeconds(opSec).ToString(@"hh\:mm\:ss"),
                    Downtime = TimeSpan.FromSeconds(downSec).ToString(@"hh\:mm\:ss"),
                    AverageCycleTime = cycle
                };
            }
            catch (Exception ex)
            {
                Log.Write(_logFile, "PLC Read Error: " + ex.Message);
                _plcClient?.Dispose();
                _plcClient = null;
                _plcReader = null;
                _plcWriter = null;
                return null;
            }
        }

        #endregion

        #region WPF TCP Server

        private async Task StartTcpServerAsync(CancellationToken stoppingToken)
        {
            try
            {
                _tcpServer = new TcpListener(IPAddress.Loopback, _wpfPort);
                _tcpServer.Start();
                Log.Write(_logFile, $"TCP Server started on port {_wpfPort}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_tcpServer.Pending())
                    {
                        var client = await _tcpServer.AcceptTcpClientAsync(stoppingToken);
                        _clientWriter = new StreamWriter(client.GetStream()) { AutoFlush = true };
                        Log.Write(_logFile, "WPF client connected");

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                while (client.Connected && !stoppingToken.IsCancellationRequested)
                                {
                                    await Task.Delay(100, stoppingToken);
                                }
                            }
                            finally
                            {
                                client.Close();
                                Log.Write(_logFile, "WPF client disconnected");
                            }
                        }, stoppingToken);
                    }
                    else
                    {
                        await Task.Delay(100, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(_logFile, "TCP Server Error: " + ex.Message);
            }
        }

        #endregion
    }

    #region Logger

    public static class Log
    {
        private static readonly object _lock = new();

        public static void Write(string filePath, string message)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    File.AppendAllText(filePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\n");
                }
            }
            catch { }
        }
    }

    #endregion
}
