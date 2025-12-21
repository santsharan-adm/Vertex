using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks; // Ensure this is present for Task
using System.Windows;
using System.Windows.Interop;
using System.Threading.Tasks;
using System;

using IPCSoftware.Shared.Models.Messaging;
using IPCSoftware.Services;

namespace IPCSoftware.App.Services.UI
{
    public class UiTcpClient: BaseService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly IDialogService _dialog;
      
        private bool _hasShownError = false;

        // NEW: Persistent buffer for accumulating message fragments
        private readonly StringBuilder _messageAccumulator = new StringBuilder();

        public bool IsConnected => _client?.Connected ?? false;

        public event Action<string> DataReceived;
        public event Action<bool> UiConnected;

        // This event is defined here but only raised in CoreClient.cs
        public event Action<AlarmMessage> AlarmMessageReceived;

        public UiTcpClient(IDialogService dialog,
            IAppLogger logger):base(logger)
        {
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
                Application.Current?.Dispatcher.Invoke(() => UiConnected?.Invoke(true));
                _ = Task.Run(ReadLoop); 
                UiConnected?.Invoke(true);
                _hasShownError = false;
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

                _logger.LogError(ex.Message, LogType.Diagnostics);


                // Show error only once
                if (!_hasShownError)
                {
                    // CRITICAL FIX: Marshaling the Dialog call to the UI Thread (STA)
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        _dialog.ShowWarning($"TCP ERROR: {ex.Message}. Retrying...");
                    });
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
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    string dataChunk = Encoding.UTF8.GetString(buffer, 0, read);
                    _messageAccumulator.Append(dataChunk);

                    // Process accumulated data and extract complete messages
                    while (true)
                    {
                        string currentBuffer = _messageAccumulator.ToString();
                        int newlineIndex = currentBuffer.IndexOf('\n');

                        if (newlineIndex >= 0)
                        {
                            string rawMessage = currentBuffer.Substring(0, newlineIndex);
                            string completeMessage = rawMessage.Trim(); // ✅ Trim off the \r for parsing

                            // 4. Send the complete, cleaned message for UI processing
                            DataReceived?.Invoke(completeMessage);

                            // 5. CRITICAL FIX: Remove the length of the JSON + delimiters (\r\n)
                            int charsToRemove = newlineIndex + 1; // Start by including the '\n'

                            // Check if the character before '\n' is '\r'.
                            if (newlineIndex > 0 && currentBuffer[newlineIndex - 1] == '\r')
                            {
                                charsToRemove++; // If \r is present, remove one more character.
                            }

                            _messageAccumulator.Remove(0, charsToRemove); // ⬅️ Correct removal length
                        }
                        else
                        {
                            // No more complete messages in the buffer
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Read error: {ex.Message}");
                _logger.LogError($"Read error: {ex.Message}", LogType.Diagnostics);
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
                _logger.LogInfo("Cannot send: Not connected", LogType.Diagnostics);
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
                _logger.LogError($"Send error: {ex.Message}", LogType.Diagnostics);
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
            catch (Exception ex)
            {
                _logger.LogError($"Dispose error: {ex.Message}", LogType.Diagnostics);
            }

            _stream = null;
            _client = null;
        }
    }
}