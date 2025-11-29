using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SimulatedPLCService
{
    class Program
    {
        private static readonly string LogFile = @"C:\CoreServiceLogs\plc.log";

        static async Task Main(string[] args)
        {
            Log.Write(LogFile, "PLC Service Starting...");

            // Create TCP server on port 6000
            TcpListener listener = new TcpListener(IPAddress.Loopback, 6000);
            listener.Start();
            Log.Write(LogFile, "PLC Service listening on port 6000");

            int operatingSeconds = 0;
            int downtimeSeconds = 0;
            double avgCycle = 5.8;
            Random rnd = new Random();

            while (true)
            {
                try
                {
                    // Increment counters
                    operatingSeconds++;
                    if (operatingSeconds % 120 == 0)
                        downtimeSeconds += 5;

                    avgCycle = 5.5 + rnd.NextDouble(); // Simulated cycle time

                    string packet = $"OPERATING={operatingSeconds};DOWN={downtimeSeconds};CYCLE={avgCycle:F2}";

                    Log.Write(LogFile, $"Waiting for CoreService connection...");

                    // Accept a client (CoreService)
                    using (TcpClient client = await listener.AcceptTcpClientAsync())
                    using (StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true })
                    {
                        Log.Write(LogFile, $"CoreService connected, sending packet: {packet}");
                        await writer.WriteLineAsync(packet);
                    }

                    Log.Write(LogFile, $"Packet sent: {packet}");

                    await Task.Delay(1000); // 1-second interval
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
}
