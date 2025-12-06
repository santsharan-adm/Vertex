using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json; // Required for JSON serialization/deserialization
using System.IO;

namespace IPCSoftware.App.Services
{
    public class UiTcpClient : IDisposable
    {
        // Event to notify CoreClient when a complete JSON object is received
        public event Action<ResponsePackage>? OnPackageReceived;

        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private Task? _readTask;
        private CancellationTokenSource? _cts;

        private readonly string _ipAddress;
        private readonly int _port;

        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        public Action<string> DataReceived { get; internal set; }

        public UiTcpClient(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public void Connect()
        {
            if (IsConnected) return;

            try
            {
                // Synchronous/Blocking connection attempt (as per original file structure)
                _tcpClient = new TcpClient();
                _tcpClient.Connect(IPAddress.Parse(_ipAddress), _port);

                _stream = _tcpClient.GetStream();

                // Start the asynchronous reading task immediately after connection success
                StartReadingAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR] Failed to connect to CoreService: {ex.Message}");
                Dispose(); // Clean up if connection fails
                throw;
            }
        }

        public void Disconnect()
        {
            // Signal the reading task to stop
            _cts?.Cancel();

            if (_stream != null)
            {
                _stream.Close();
                _stream.Dispose();
                _stream = null;
            }
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
                _tcpClient = null;
            }
        }

        // Synchronous method to send a JSON request (used for Writes or specific Reads)
        public void Send(string jsonRequest)
        {
            if (!IsConnected || _stream == null)
            {
                throw new InvalidOperationException("Cannot send data; connection is not active.");
            }

            // Add mandatory newline delimiter
            string finalRequest = jsonRequest + "\n";
            byte[] buffer = Encoding.UTF8.GetBytes(finalRequest);

            _stream.Write(buffer, 0, buffer.Length);
            // Console.WriteLine($"[DEBUG] Sent: {finalRequest.Trim()}"); 
        }


        // ---------------------------------------------------------
        // ASYNCHRONOUS READING LOOP (Handles continuous stream reading and parsing)
        // ---------------------------------------------------------
        private void StartReadingAsync()
        {
            if (_readTask != null && !_readTask.IsCompleted) return;
            if (_stream == null) return;

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _readTask = Task.Run(async () =>
            {
                byte[] buffer = new byte[4096];
                StringBuilder sb = new StringBuilder();

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // 1. Read bytes from the stream
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (bytesRead == 0)
                        {
                            throw new IOException("Connection gracefully closed by server.");
                        }

                        // 2. Accumulate and decode received bytes
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                        // 3. Process complete JSON messages delimited by newline ('\n')
                        while (true)
                        {
                            string accumulatedString = sb.ToString();
                            int delimiterIndex = accumulatedString.IndexOf('\n');

                            if (delimiterIndex == -1) break; // No complete message found

                            string json = accumulatedString.Substring(0, delimiterIndex).Trim();
                            sb.Remove(0, delimiterIndex + 1); // Remove the message and the delimiter

                            if (string.IsNullOrWhiteSpace(json)) continue;

                            try
                            {
                                // Deserialize the complete JSON object
                                ResponsePackage? response = JsonSerializer.Deserialize<ResponsePackage>(json);

                                if (response != null)
                                {
                                    // Notify the CoreClient layer that a package has arrived
                                    OnPackageReceived?.Invoke(response);
                                }
                            }
                            catch (JsonException)
                            {
                                // Customer Guideline: Robust Error Handling
                                // If deserialization fails (corrupted JSON), log and discard, but keep reading.
                                // Console.WriteLine($"[ERROR] Failed to deserialize JSON: {json}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal exit during application shutdown
                }
                catch (Exception ex)
                {
                    // Log any critical failure in the reading task
                    Console.WriteLine($"[FATAL READ ERROR] Reading task terminated: {ex.Message}");
                }
                finally
                {
                    // Ensure full cleanup if the background reading loop stops
                    Dispose();
                }
            }, token);
        }

        public void Dispose()
        {
            // Signal the reading task to stop
            _cts?.Cancel();

            // Wait for the task to finish before disconnecting
            try
            {
                _readTask?.Wait(200);
            }
            catch (Exception) { }

            // Perform final cleanup
            Disconnect();
            _cts?.Dispose();
        }
    }
}