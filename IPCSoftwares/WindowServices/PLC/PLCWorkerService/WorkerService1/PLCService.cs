using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A background service that connects to a PLC over TCP/IP (Modbus),
/// reads registers periodically, and sends the data to a CoreService via a named pipe.
/// Configuration (PLC connection, logging, pipe) is injected via appsettings.json.
/// </summary>
public class PLCService : BackgroundService
{
    private readonly ILogger<PLCService> _logger;

    // PLC configuration settings injected via IOptions
    private readonly PLCSettings _plcSettings;

    // Logging configuration settings injected via IOptions
    private readonly LoggingSettings _loggingSettings;

    // Named pipe configuration settings injected via IOptions
    private readonly PipeSettings _pipeSettings;

    private TcpClient _plcClient;      // TCP client for PLC connection
    private NetworkStream _plcStream;  // Network stream for PLC communication

    /// <summary>
    /// Constructor initializes dependencies and ensures log folder exists.
    /// </summary>
    public PLCService(
        ILogger<PLCService> logger,
        IOptions<PLCSettings> plcOptions,
        IOptions<LoggingSettings> loggingOptions,
        IOptions<PipeSettings> pipeOptions)
    {
        _logger = logger;
        _plcSettings = plcOptions.Value;
        _loggingSettings = loggingOptions.Value;
        _pipeSettings = pipeOptions.Value;

        // Ensure the log folder exists
        string folder = Path.GetDirectoryName(_loggingSettings.LogFilePath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }

    /// <summary>
    /// Main execution loop of the background service.
    /// Connects to the PLC, reads registers, and sends data to CoreService periodically.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log("PLC Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ensure PLC connection is active
                if (_plcClient == null || !_plcClient.Connected)
                {
                    await ConnectToPLCAsync();
                }

                // Read holding registers from PLC
                ushort[] registers = await ReadPLCRegistersAsync(_plcSettings.RegisterCount);

                // Convert registers to byte array for pipe transmission
                byte[] data = new byte[registers.Length * 2];
                for (int i = 0; i < registers.Length; i++)
                    Array.Copy(BitConverter.GetBytes(registers[i]), 0, data, i * 2, 2);

                // Send the data to CoreService via named pipe
                await SendToCoreServiceAsync(data, registers);
            }
            catch (Exception ex)
            {
                // Log the error and dispose PLC connection to retry
                Log("Error: " + ex.Message);
                DisposePLCConnection();
            }

            // Wait for 1 second before next iteration
            await Task.Delay(1000, stoppingToken);
        }
    }

    /// <summary>
    /// Establishes a TCP connection to the PLC.
    /// </summary>
    private async Task ConnectToPLCAsync()
    {
        try
        {
            _plcClient = new TcpClient
            {
                ReceiveTimeout = 2000, // 2-second receive timeout
                SendTimeout = 2000     // 2-second send timeout
            };

            await _plcClient.ConnectAsync(_plcSettings.Ip, _plcSettings.Port);
            _plcStream = _plcClient.GetStream();
            Log($"Connected to PLC at {_plcSettings.Ip}:{_plcSettings.Port}");
        }
        catch (Exception ex)
        {
            // Dispose connection if failed and rethrow exception
            DisposePLCConnection();
            throw new Exception("Failed to connect to PLC: " + ex.Message);
        }
    }

    /// <summary>
    /// Reads a specified number of holding registers from the PLC using Modbus TCP.
    /// </summary>
    /// <param name="count">Number of registers to read</param>
    /// <returns>Array of ushort values read from the PLC</returns>
    private async Task<ushort[]> ReadPLCRegistersAsync(int count)
    {
        if (_plcClient == null || !_plcClient.Connected)
            throw new Exception("Not connected to PLC.");

        // Build Modbus TCP request for reading holding registers
        byte[] request = new byte[]
        {
            0x00, 0x01,       // Transaction ID
            0x00, 0x00,       // Protocol ID
            0x00, 0x06,       // Length
            0x01,             // Unit ID
            0x03,             // Function code: Read Holding Registers
            0x00, 0x00,       // Start address
            (byte)(count >> 8), (byte)(count & 0xFF) // Number of registers
        };

        await _plcStream.WriteAsync(request, 0, request.Length);

        // Read full Modbus TCP response
        int expectedBytes = 9 + count * 2; // 9-byte header + 2 bytes per register
        byte[] response = new byte[expectedBytes];
        int totalRead = 0;

        while (totalRead < expectedBytes)
        {
            int read = await _plcStream.ReadAsync(response, totalRead, expectedBytes - totalRead);
            if (read == 0) throw new Exception("PLC closed the connection.");
            totalRead += read;
        }

        // Parse registers from response bytes
        ushort[] registers = new ushort[count];
        for (int i = 0; i < count; i++)
            registers[i] = (ushort)(response[9 + i * 2] << 8 | response[10 + i * 2]);

        return registers;
    }

    /// <summary>
    /// Sends PLC register data to CoreService via named pipe.
    /// </summary>
    private async Task SendToCoreServiceAsync(byte[] data, ushort[] registers)
    {
        try
        {
            using (var pipeClient = new NamedPipeClientStream(".", _pipeSettings.PipeName, PipeDirection.Out))
            {
                Log("Connecting to CoreService...");
                await pipeClient.ConnectAsync(2000); // 2-second timeout

                await pipeClient.WriteAsync(data, 0, data.Length);
                await pipeClient.FlushAsync();

                Log("Sent PLC data to CoreService: " + string.Join(", ", registers));
            }
        }
        catch (TimeoutException)
        {
            Log("Could not connect to CoreService, will retry.");
        }
        catch (Exception ex)
        {
            Log("Error sending to CoreService: " + ex.Message);
        }
    }

    /// <summary>
    /// Safely disposes the TCP connection to the PLC.
    /// </summary>
    private void DisposePLCConnection()
    {
        try { _plcStream?.Dispose(); } catch { }
        try { _plcClient?.Close(); } catch { }
        _plcStream = null;
        _plcClient = null;
    }

    /// <summary>
    /// Logs messages to both the configured log file and the injected ILogger.
    /// </summary>
    private void Log(string message)
    {
        try
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            File.AppendAllText(_loggingSettings.LogFilePath, logEntry + Environment.NewLine);
            _logger?.LogInformation(message);
        }
        catch { }
    }

    /// <summary>
    /// Stops the service and disposes the PLC connection gracefully.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        DisposePLCConnection();
        Log("PLC Service stopped.");
        await base.StopAsync(cancellationToken);
    }
}
