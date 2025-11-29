using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class PLCWorkerTest : BackgroundService
{
    private readonly ILogger<PLCWorkerTest> _logger;
    private const string PipeName = "PLC_TO_CORE";
    private readonly string logFilePath = @"C:\CoreServiceLogs\PLCWorkerTest.log";
    private readonly Random _random = new Random();

    public PLCWorkerTest(ILogger<PLCWorkerTest> logger)
    {
        _logger = logger;

        // Ensure log folder exists
        string folder = Path.GetDirectoryName(logFilePath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log("PLC Worker Client started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Generate 10 random ushort values
                ushort[] registers = Enumerable.Range(0, 10)
                    .Select(_ => (ushort)_random.Next(0, 101))
                    .ToArray();

                byte[] data = new byte[registers.Length * 2];
                for (int i = 0; i < registers.Length; i++)
                    Array.Copy(BitConverter.GetBytes(registers[i]), 0, data, i * 2, 2);

                using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    Log("Connecting to CoreService...");
                    await pipeClient.ConnectAsync(2000, stoppingToken); 

                    await pipeClient.WriteAsync(data, 0, data.Length, stoppingToken);
                    await pipeClient.FlushAsync(stoppingToken);

                    Log("Sent random values: " + string.Join(", ", registers));
                }
            }
            catch (TimeoutException)
            {
                Log("Could not connect to CoreService, retrying next second...");
            }
            catch (Exception ex)
            {
                Log("Error sending data: " + ex.Message);
            }

            await Task.Delay(1000, stoppingToken); 
        }
    }

    private void Log(string message)
    {
        try
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }
        catch { }
    }
}
