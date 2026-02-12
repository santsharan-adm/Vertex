using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared
{
    public class MacMiniTcpClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private string _connectedHost;
        private int _connectedPort;

        public bool IsConnected => _client != null && _client.Connected;

        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                // If connecting to the same host/port and it's alive, reuse it
                if (IsConnected && _connectedHost == host && _connectedPort == port)
                {
                    if (IsSocketConnected(_client.Client)) return;
                }

                Disconnect(); // Ensure clean slate

                _client = new TcpClient();
                // Connect with a 3-second timeout logic could be added here if needed
                await _client.ConnectAsync(host, port);

                _stream = _client.GetStream();
                _stream.ReadTimeout = 5000; // 5s Read Timeout

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

            // 1. Clean buffers
            if (_client.Available > 0)
            {
                byte[] garbage = new byte[_client.Available];
                await _stream.ReadAsync(garbage, 0, garbage.Length);
            }

            // 2. Send Data
            byte[] data = Encoding.ASCII.GetBytes(message);
            await _stream.WriteAsync(data, 0, data.Length);

            // 3. Read Response
            byte[] buffer = new byte[8192];
            try
            {
                // ReadAsync will wait until data arrives or connection closes
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    Disconnect(); // Server closed connection
                    return string.Empty;
                }

                return Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }
            catch (Exception)
            {
                // Timeout or socket error
                Disconnect();
                throw;
            }
        }

        /// <summary>
        /// Checks if the socket is actually connected by polling.
        /// </summary>
        private bool IsSocketConnected(Socket s)
        {
            if (s == null) return false;
            // Poll returns true if:
            // A) connection is closed, reset, terminated (check Available==0)
            // B) connection is active and there is data to read
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2) return false; // Connection closed
            return true;
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
}
