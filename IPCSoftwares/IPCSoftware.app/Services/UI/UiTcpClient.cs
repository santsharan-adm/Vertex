// ============================================
// 1. UiTcpClient.cs - COMPLETE FIX
// ============================================
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace IPCSoftware.App.Services.UI
{
    public class UiTcpClient : BaseService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly IDialogService _dialog;
        private bool _hasShownError = false;
        private readonly StringBuilder _messageAccumulator = new StringBuilder();

        // CRITICAL FIX: Track if ReadLoop is running
        private CancellationTokenSource _readLoopCts;
        private Task _readLoopTask;

        public bool IsConnected => _client?.Connected ?? false;

        public event Action<string> DataReceived;
        public event Action<bool> UiConnected;
        public event Action<AlarmMessage> AlarmMessageReceived;

        public UiTcpClient(IDialogService dialog, IAppLogger logger) : base(logger)
        {
            _dialog = dialog;
        }

        public async Task<bool> StartAsync(string ip, int port)
        {
            try
            {
                // CRITICAL FIX: Always cleanup before attempting new connection
                await CleanupAsync();

                _client = new TcpClient();

                // CRITICAL FIX: Set socket options for better cleanup
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // Connection timeout
                var connectTask = _client.ConnectAsync(ip, port);
                if (await Task.WhenAny(connectTask, Task.Delay(3000)) != connectTask)
                {
                    _client?.Close();
                    throw new TimeoutException("Connection attempt timed out");
                }

                await connectTask;

                _stream = _client.GetStream();
                _hasShownError = false;

                // CRITICAL FIX: Create new cancellation token for this connection
                _readLoopCts = new CancellationTokenSource();

                // Start read loop with cancellation support
                _readLoopTask = Task.Run(() => ReadLoop(_readLoopCts.Token));

                // Fire connected event
                Application.Current?.Dispatcher.InvokeAsync(() => UiConnected?.Invoke(true));

                _logger.LogInfo($"✅ TCP Connected to {ip}:{port}", LogType.Diagnostics);

                return true;
            }
            catch (Exception ex)
            {
                await CleanupAsync();
                UiConnected?.Invoke(false);

                _logger.LogError($"[TCP_CONNECT_ERROR] {ex.Message}", LogType.Diagnostics);

                if (!_hasShownError)
                {
                    _hasShownError = true;
                    Application.Current?.Dispatcher.InvokeAsync(() =>
                    {
                        _dialog.ShowWarning("Core Service unavailable. Retrying in background...");
                    });
                }

                return false;
            }
        }

        private async Task ReadLoop(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    // CRITICAL FIX: Use ReadAsync with cancellation token
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (read <= 0)
                    {
                        _logger.LogWarning("Read 0 bytes - connection closed", LogType.Diagnostics);
                        break;
                    }

                    string dataChunk = Encoding.UTF8.GetString(buffer, 0, read);
                    _messageAccumulator.Append(dataChunk);

                    while (true)
                    {
                        string currentBuffer = _messageAccumulator.ToString();
                        int newlineIndex = currentBuffer.IndexOf('\n');

                        if (newlineIndex >= 0)
                        {
                            string rawMessage = currentBuffer.Substring(0, newlineIndex);
                            string completeMessage = rawMessage.Trim();

                            DataReceived?.Invoke(completeMessage);

                            int charsToRemove = newlineIndex + 1;
                            if (newlineIndex > 0 && currentBuffer[newlineIndex - 1] == '\r')
                            {
                                charsToRemove++;
                            }

                            _messageAccumulator.Remove(0, charsToRemove);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("ReadLoop cancelled", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ReadLoop error: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                _logger.LogInfo("ReadLoop exiting - firing disconnect event", LogType.Diagnostics);
                UiConnected?.Invoke(false);
                await CleanupAsync();
            }
        }

        public async Task Send(string message)
        {
            if (_stream == null || !IsConnected)
            {
                _logger.LogWarning("Cannot send: Not connected", LogType.Diagnostics);
                return;
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message + "\n");
                _stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Send error: {ex.Message}", LogType.Diagnostics);
            }
        }

        // CRITICAL FIX: Async cleanup with proper disposal
        private async Task CleanupAsync()
        {
            try
            {
                // Cancel read loop
                _readLoopCts?.Cancel();

                // Wait for read loop to exit (with timeout)
                if (_readLoopTask != null && !_readLoopTask.IsCompleted)
                {
                    await Task.WhenAny(_readLoopTask, Task.Delay(1000));
                }

                _readLoopCts?.Dispose();
                _readLoopCts = null;
                _readLoopTask = null;

                // Close stream
                if (_stream != null)
                {
                    try
                    {
                        _stream.Close();
                        _stream.Dispose();
                    }
                    catch { }
                    _stream = null;
                }

                // Close client
                if (_client != null)
                {
                    try
                    {
                        _client.Close();
                        
                        _client.Dispose();
                    }
                    catch { }
                    _client = null;
                }

                _messageAccumulator.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cleanup error: {ex.Message}", LogType.Diagnostics);
            }
        }
    }
}