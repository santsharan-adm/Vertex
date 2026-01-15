using System.Net.Sockets;
using System.Text;

namespace IPCSoftware.CoreService.Network
{
    public abstract class BaseTcpClient
    {
        protected readonly string _host;
        protected readonly int _port;

        protected TcpClient _client;
        protected NetworkStream _stream;

        public Func<string, Task> OnMessageReceived;

        private CancellationTokenSource _cts;

        protected BaseTcpClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync();
                    _ = Task.Run(() => ReceiveLoop(_cts.Token));

                    return; 
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }

        public async Task<bool> ConnectAsync()
        {
            if (_client?.Connected == true)
                return true;

            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);

            _stream = _client.GetStream();
            return true;
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            while (!token.IsCancellationRequested)
            {
                int read = await _stream.ReadAsync(buffer, token);
                if (read <= 0) break;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

                // Process messages separated by newline
                while (sb.ToString().Contains("\n"))
                {
                    string full = ExtractMessage(ref sb);
                    if (OnMessageReceived != null)
                        await OnMessageReceived(full.Trim());
                }
            }
        }

        private string ExtractMessage(ref StringBuilder sb)
        {
            string full = sb.ToString();
            int idx = full.IndexOf("\n");

            string msg = full.Substring(0, idx);
            sb.Remove(0, idx + 1);

            return msg;
        }

        public async Task SendAsync(string json)
        {
            if (_stream == null) return;

            json = json + "\n"; // VERY IMPORTANT
            byte[] data = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(data);
        }
    }
}
