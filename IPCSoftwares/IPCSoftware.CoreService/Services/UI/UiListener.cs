using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using System.Collections.Concurrent; // Needed for thread-safe stream management
using System.ComponentModel.Design;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;       // Needed for Task

namespace IPCSoftware.CoreService.Services.UI
{
    // 🚨 FINAL STRUCTURAL FIX: UiListener must implement IMessagePublisher
    public class UiListener : BaseService, IMessagePublisher
    {
        private readonly int _port;
        private TcpListener _listener;

        // 🚨 CRITICAL ADDITION: Thread-safe storage for all active streams
        private readonly ConcurrentDictionary<Guid, NetworkStream> _activeStreams = new ConcurrentDictionary<Guid, NetworkStream>();

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
                    Console.WriteLine("UI CLIENT CONNECTED");
                    _logger.LogInfo("UI CLIENT CONNECTED",LogType.Diagnostics);

                    // Assign a unique ID to track this client's stream
                    Guid clientId = Guid.NewGuid();
                    NetworkStream stream = client.GetStream();

                    // Add the stream to the active list
                    _activeStreams.TryAdd(clientId, stream);

                    _ = Task.Run(() => HandleClientAsync(client, clientId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR IN UILISTENER:{ ex.Message}", LogType.Diagnostics);
            }
        }


        private async Task HandleClientAsync(TcpClient client, Guid clientId)
        {
            // Use the stream associated with this client
            if (!_activeStreams.TryGetValue(clientId, out var stream)) return;

            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            Console.WriteLine("UI client connected");
            _logger.LogInfo("UI client connected", LogType.Diagnostics);

            try
            {
                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0) break; // Client disconnected

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
                            _logger.LogError("Invalid JSON from UI: " + json, LogType.Diagnostics);
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

                        // Response path: Delimiter is already correctly handled here
                        string outJson = MessageSerializer.Serialize(response) + "\n";
                        Console.WriteLine("SENDING TO UI: " + outJson.Trim());
                      //      _logger.LogInfo("SENDING TO UI: " + outJson.Trim(), LogType.Diagnostics);
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(outJson));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UI listener error: " + ex.Message, LogType.Diagnostics);
            }
            finally
            {
                // Ensure stream is removed on disconnection
                _activeStreams.TryRemove(clientId, out _);
                stream?.Dispose();
                client?.Close();
            }
        }





        // 🚨 CRITICAL IMPLEMENTATION: IMessagePublisher method (Modified Error Handling)
        public async Task PublishAsync<T>(T message)
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


        private string ExtractMessage(ref StringBuilder sb)
        {
            string text = sb.ToString();
            int idx = text.IndexOf("\n");

            string msg = text.Substring(0, idx);
            sb.Remove(0, idx + 1);

            return msg.Trim(); // Trim the extracted message for safety
        }
    }
}