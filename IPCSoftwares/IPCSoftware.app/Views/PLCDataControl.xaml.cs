using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
{
    public partial class PLCDataControl : UserControl
    {
        private CancellationTokenSource _cts;
        private readonly string LogFile = @"C:\WPFLogs\wpf.log";

        public PLCDataControl()
        {
            InitializeComponent();
            Loaded += PLCDataControl_Loaded;
            Unloaded += PLCDataControl_Unloaded;
        }

        private void PLCDataControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ConnectAsync(_cts.Token));
        }

        private void PLCDataControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private async Task ConnectAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Log.Write(LogFile, "Connecting to CoreService...");

                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync("127.0.0.1", 5050);

                        Log.Write(LogFile, "Connected to CoreService");

                        using (var reader = new StreamReader(client.GetStream()))
                        {
                            while (!token.IsCancellationRequested)
                            {
                                string json = await reader.ReadLineAsync();
                                Log.Write(LogFile, "Received JSON: " + json);

                                if (string.IsNullOrWhiteSpace(json)) continue;

                                var data = JsonSerializer.Deserialize<DashboardData>(json);

                                Dispatcher.Invoke(() =>
                                {
                                    txtOperatingTime.Text = data.OperatingTime;
                                    txtDowntime.Text = data.Downtime;
                                    txtAverageCycleTime.Text = data.AverageCycleTime.ToString("F2");
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(LogFile, "Error: " + ex.Message);
                    await Task.Delay(1000);
                }
            }
        }
    }

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

    public class DashboardData
    {
        public string OperatingTime { get; set; }
        public string Downtime { get; set; }
        public double AverageCycleTime { get; set; }
    }
}
