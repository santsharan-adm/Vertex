
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Services.UI
{
    // 🚨 FINAL STRUCTURAL FIX: UiListener must implement IMessagePublisher
    public class UiListener : BaseService, IMessagePublisher
    {
        private readonly int _port;
        private TcpListener _listener;

        // 🚨 CRITICAL ADDITION: Thread-safe storage for all active streams
      //  private readonly ConcurrentDictionary<Guid, NetworkStream> _activeStreams = new ConcurrentDictionary<Guid, NetworkStream>();
        private readonly ConcurrentDictionary<Guid, ClientConnection> _activeClients = new();

        public Func<RequestPackage, Task<ResponsePackage>>? OnRequestReceived;

        public UiListener(int port,IAppLogger logger) : base (logger)
        {
            _port = port;
        }

        public async Task StartAsync()
        {
            _logger.LogInfo("Starting UI Listener...",LogType.Diagnostics);

            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                _logger.LogInfo($"UI Listener started on port {_port}",LogType.Diagnostics);

                while (true)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Guid clientId = Guid.NewGuid();

                    // CRITICAL FIX: Store both TcpClient and NetworkStream
                    var clientConnection = new ClientConnection
                    {
                        Id = clientId,
                        Client = client,
                        Stream = client.GetStream(),
                        ConnectedAt = DateTime.Now
                    };

                    _activeClients.TryAdd(clientId, clientConnection);
                    _logger.LogInfo($"UI CLIENT CONNECTED (ID: {clientId:N}, Total: {_activeClients.Count})", LogType.Diagnostics);
                    Console.WriteLine("UI CLIENT CONNECTED");

                    _ = Task.Run(() => HandleClientAsync(clientConnection));

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR IN UILISTENER:{ ex.Message}", LogType.Diagnostics);
            }
        }


        private async Task HandleClientAsync(ClientConnection connection)
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            try
            {
                while (connection.Client.Connected)
                {
                    int read = await connection.Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        _logger.LogInfo($"Client {connection.Id:N} disconnected (read 0 bytes)", LogType.Diagnostics);
                        break;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

                    while (sb.ToString().Contains("\n"))
                    {
                        string json = ExtractMessage(ref sb);
                        if (string.IsNullOrWhiteSpace(json)) continue;

                        RequestPackage? request = null;

                        try
                        {
                            request = MessageSerializer.Deserialize<RequestPackage>(json);
                        }
                        catch
                        {
                            _logger.LogError($"Invalid JSON from client {connection.Id:N}: {json}", LogType.Diagnostics);
                            continue;
                        }

                        ResponsePackage response;

                        if (OnRequestReceived != null)
                        {
                            response = await OnRequestReceived(request);
                        }
                        else
                        {
                            response = new ResponsePackage { ResponseId = -1 };
                        }

                        string outJson = MessageSerializer.Serialize(response) + "\n";

                        // CRITICAL FIX: Check if still connected before writing
                        if (connection.Client.Connected)
                        {
                            try
                            {
                                await connection.Stream.WriteAsync(Encoding.UTF8.GetBytes(outJson));
                            }
                            catch (Exception writeEx)
                            {
                                _logger.LogError($"Write error to client {connection.Id:N}: {writeEx.Message}", LogType.Diagnostics);
                                break;
                            }
                        }
                        else
                        {
                            _logger.LogInfo($"Client {connection.Id:N} disconnected during response", LogType.Diagnostics);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Client {connection.Id:N} error: {ex.Message}", LogType.Diagnostics);
            }
            finally
            {
                // CRITICAL FIX: Proper cleanup
                CleanupClient(connection);
            }
        }

        private void CleanupClient(ClientConnection connection)
        {
            try
            {
                _activeClients.TryRemove(connection.Id, out _);

                connection.Stream?.Close();
                connection.Stream?.Dispose();
                connection.Client?.Close();
                connection.Client?.Dispose();

                _logger.LogInfo($"Client {connection.Id:N} cleaned up (Total remaining: {_activeClients.Count})", LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cleanup error for client {connection.Id:N}: {ex.Message}", LogType.Diagnostics);
            }
        }


        public async Task PublishAsync<T>(T message)
        {
            if (_activeClients.Count == 0)
            {
                _logger.LogWarning("No active clients to publish alarm message", LogType.Diagnostics);
                return;
            }

            string outJson = MessageSerializer.Serialize(message) + "\n";
            byte[] bytes = Encoding.UTF8.GetBytes(outJson);

            _logger.LogInfo($"Publishing message to {_activeClients.Count} clients: {outJson.Trim()}", LogType.Diagnostics);

            var disconnectedClients = new List<ClientConnection>();

            foreach (var pair in _activeClients.ToArray())
            {
                var connection = pair.Value;

                try
                {
                    if (connection.Client.Connected)
                    {
                        await connection.Stream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        disconnectedClients.Add(connection);
                    }
                }
                catch (IOException ex) when (ex.InnerException is SocketException se &&
                    se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    _logger.LogWarning($"Client {connection.Id:N} disconnected during publish", LogType.Diagnostics);
                    disconnectedClients.Add(connection);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Publish error to client {connection.Id:N}: {ex.Message}", LogType.Diagnostics);
                    disconnectedClients.Add(connection);
                }
            }

            // Clean up disconnected clients
            foreach (var client in disconnectedClients)
            {
                CleanupClient(client);
            }
        }



        // 🚨 CRITICAL IMPLEMENTATION: IMessagePublisher method (Modified Error Handling)
        /* public async Task PublishAsync<T>(T message)
         {
             string outJson = MessageSerializer.Serialize(message);
             outJson += "\n";

             byte[] bytes = Encoding.UTF8.GetBytes(outJson);

             _logger.LogInfo($"ALARM PUSHING TO UI ({_activeStreams.Count} clients): {outJson.Trim()}", LogType.Diagnostics);

             // Send to all connected streams
             foreach (var pair in _activeStreams.ToList()) // Use ToList() for thread safety during iteration
             {
                 Guid clientId = pair.Key;
                 NetworkStream stream = pair.Value;

                 try
                 {
                     // Use WriteAsync with CancellationToken if possible, but standard is fine here
                     await stream.WriteAsync(bytes, 0, bytes.Length);
                 }
                 catch (IOException ex) when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionReset)
                 {
                     // The client disconnected while we were writing. Clean up this client.
                     _logger.LogError($"Client {clientId} disconnected during alarm push. Removing.", LogType.Diagnostics);
                     _activeStreams.TryRemove(clientId, out _);
                     stream.Dispose();
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError($"Alarm push error to client {clientId}: {ex.Message}", LogType.Diagnostics);
                     // General exception, try to remove and dispose to prevent future failures
                     _activeStreams.TryRemove(clientId, out _);
                     stream.Dispose();
                 }
             }
         }
         // ...
 */

        private string ExtractMessage(ref StringBuilder sb)
        {
            string text = sb.ToString();
            int idx = text.IndexOf("\n");

            string msg = text.Substring(0, idx);
            sb.Remove(0, idx + 1);

            return msg.Trim(); // Trim the extracted message for safety
        }


        private class ClientConnection
        {
            public Guid Id { get; set; }
            public TcpClient Client { get; set; }
            public NetworkStream Stream { get; set; }
            public DateTime ConnectedAt { get; set; }
        }
    }

}