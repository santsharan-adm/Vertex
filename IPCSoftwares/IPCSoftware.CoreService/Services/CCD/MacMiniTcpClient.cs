using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/*namespace IPCSoftware.CoreService.Services.CCD
{
    public class MacMiniTcpClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private string _connectedHost;
        private int _connectedPort;

        // Check if client exists and socket is connected
        public bool IsConnected => _client != null && _client.Connected;

        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                // If already connected to the same host/port, do nothing
                if (IsConnected && _connectedHost == host && _connectedPort == port)
                {
                    // Optional: Check if connection is actually alive by polling
                    if (!IsSocketConnected(_client.Client))
                    {
                        Disconnect();
                    }
                    else
                    {
                        return;
                    }
                }

                // If connected to different host or dead, disconnect first
                Disconnect();

                _client = new TcpClient();

                // Connect with a timeout logic if needed, or default
                await _client.ConnectAsync(host, port);

                _stream = _client.GetStream();
                _connectedHost = host;
                _connectedPort = port;
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        public async Task<string> SendAndReceiveAsync(string message)
        {
            if (!IsConnected || _stream == null) throw new InvalidOperationException("TCP Client not connected.");

            // 1. Send Data
            byte[] data = Encoding.ASCII.GetBytes(message);
            await _stream.WriteAsync(data, 0, data.Length);

            // 2. Read Response
            // Reading until data is available. 
            // Warning: This assumes the server sends data back immediately and closes or we read a chunk.
            // For a persistent connection without specific delimiters (like \n), we read what's available.

            byte[] buffer = new byte[8192];

            // Set read timeout
            _stream.ReadTimeout = 5000;

            try
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) return string.Empty; // Disconnected remote
                return Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }
            catch (Exception)
            {
                // Timeout or error
                throw;
            }
        }

        private bool IsSocketConnected(Socket s)
        {
            return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
        }

        public void Disconnect()
        {
            try { _stream?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }
            try { _client?.Dispose(); } catch { }

            _client = null;
            _stream = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}*/
