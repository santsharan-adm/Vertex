using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App
{
    public class UiTcpClient
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public event Action<string> DataReceived;

        public async Task StartAsync(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);

                _stream = _client.GetStream();

                // Background read loop
                _ = Task.Run(ReadLoop);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("TCP ERROR: " + ex.Message);
            }
        }

        private async Task ReadLoop()
        {
            byte[] buffer = new byte[4096];

            while (true)
            {
                try
                {
                    int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, read);
                    DataReceived?.Invoke(msg);
                }
                catch
                {
                    break;
                }
            }
        }

        public void Send(string message)
        {
            if (_stream == null)
                return;

            byte[] bytes = Encoding.UTF8.GetBytes(message + "\n");
            _stream.Write(bytes, 0, bytes.Length);
        }
    }
}
