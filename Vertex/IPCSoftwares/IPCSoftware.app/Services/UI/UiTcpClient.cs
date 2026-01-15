using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks; // Ensure this is present for Task
using System.Windows.Interop;

namespace IPCSoftware.App.Services.UI
{
    public class UiTcpClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly IDialogService _dialog;
        private readonly IAppLogger _logger;
        private bool _hasShownError = false;

        // NEW: Persistent buffer for accumulating message fragments
        private readonly StringBuilder _messageAccumulator = new StringBuilder();

        public bool IsConnected => _client?.Connected ?? false;

        public event Action<string> DataReceived;
        public event Action<bool> UiConnected;

        public UiTcpClient(IDialogService dialog, IAppLogger logger)
        {
            _logger = logger;
            _dialog = dialog;
        }

        public async Task<bool> StartAsync(string ip, int port)
        {
            try
            {
                Cleanup();
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);

                _stream = _client.GetStream();

                // Reset error flag on successful connection
                // _hasShownError = false;

                // Background read loop
                _ = Task.Run(ReadLoop);
                return true;
            }
            catch (Exception ex)
            {
                UiConnected?.Invoke(false);
                var logMessage = $"[TCP_CONNECT_ERROR] " +
                                    $"Class=UiTcpClient | " +
                                    $"Method=StartAsync | " +
                                    $"IP={ip} | Port={port} | " +
                                    $"ExceptionType={ex.GetType().Name} | " +
                                    $"Message={ex.Message.Replace(',', ';')} | ";

                _logger.LogError(logMessage, LogType.Diagnostics);


                // Show error only once
                if (!_hasShownError)
                {
                    _dialog.ShowWarning($"TCP ERROR: {ex.Message}. Retrying...");
                    _hasShownError = true;
                }
                return false;
            }
        }

        private async Task ReadLoop()
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (IsConnected)
                {
                    // 1. Read bytes into the buffer
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    // 2. Append the new data to the accumulator
                    string dataChunk = Encoding.UTF8.GetString(buffer, 0, read);
                    _messageAccumulator.Append(dataChunk);

                    // 3. Process accumulated data and extract complete messages
                    while (true)
                    {
                        string currentBuffer = _messageAccumulator.ToString();
                        int newlineIndex = currentBuffer.IndexOf('\n');

                        // Check if a complete message is available
                        if (newlineIndex >= 0)
                        {
                            // Extract the complete message (excluding the '\n')
                            string completeMessage = currentBuffer.Substring(0, newlineIndex);

                            // 4. Send the complete message for UI processing
                            DataReceived?.Invoke(completeMessage);

                            // 5. Remove the processed message (and the '\n') from the accumulator
                            _messageAccumulator.Remove(0, newlineIndex + 1);
                        }
                        else
                        {
                            // No more complete messages in the buffer, go back to reading the stream
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Read error: {ex.Message}");
                // Optional: Log read errors using your IAppLogger if desired
            }
            finally
            {
                UiConnected?.Invoke(false);
                Cleanup();
            }
        }

        public void Send(string message)
        {
            if (_stream == null || !IsConnected)
            {
                Console.WriteLine("Cannot send: Not connected");
                return;
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message + "\n");
                _stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }

        private void Cleanup()
        {
            try
            {
                _stream?.Close();
                _stream?.Dispose();
                _client?.Close();
                _client?.Dispose();

                // Clear the buffer on cleanup so old data doesn't mix with new connections
                _messageAccumulator.Clear();
            }
            catch { }

            _stream = null;
            _client = null;
        }
    }
}