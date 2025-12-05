using System.Net;
using System.Net.Sockets;
using System.Text;
using IPCSoftware.Shared.Models.Messaging;

namespace IPCSoftware.CoreService.Services.UI
{
    public class UiListener
    {
        private readonly int _port;
        private TcpListener _listener;

        public Func<RequestPackage, Task<ResponsePackage>>? OnRequestReceived;

        public UiListener(int port)
        {
            _port = port;
        }

        public async Task StartAsync()
        {
            Console.WriteLine("Starting UI Listener...");

            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                Console.WriteLine($"UI Listener started on port {_port}");

                while (true)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("UI CLIENT CONNECTED");

                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR IN UILISTENER: " + ex.Message);
            }
        }


        private async Task HandleClientAsync(TcpClient client)
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            Console.WriteLine("UI client connected");

            try
            {
                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0) break;

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

                    // process each JSON terminated by newline
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
                            Console.WriteLine("Invalid JSON from UI: " + json);
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

                        // Only ONE newline terminator
                        string outJson = MessageSerializer.Serialize(response) + "\n";

                        Console.WriteLine("SENDING TO UI: " + outJson.Trim());

                        // Write exactly ONE newline — DO NOT ADD EXTRA
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(outJson));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("UI listener error: " + ex.Message);
            }
        }

        private string ExtractMessage(ref StringBuilder sb)
        {
            string text = sb.ToString();
            int idx = text.IndexOf("\n");

            string msg = text.Substring(0, idx);
            sb.Remove(0, idx + 1);

            return msg;
        }
    }
}
