using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ComunicacionSocket
{
    public class Client
    {
        private string _ipAddress;
        private int _port;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;

        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public Client(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public bool ConnectToServer()
        {
            try
            {
                _tcpClient = new TcpClient(_ipAddress, _port);
                _networkStream = _tcpClient.GetStream();

                // Start listening for incoming data immediately
                Task.Run(() => ListenForData());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to server: {ex.Message}");
                return false;
            }
        }

        private async Task ListenForData()
        {
            try
            {
                byte[] buffer = new byte[4096];
                while (_tcpClient.Connected)
                {
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        DataReceived?.Invoke(this, new DataReceivedEventArgs(buffer, bytesRead));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data: {ex.Message}");
            }
        }

        public int SendData(byte[] data, int length)
        {
            try
            {
                if (_networkStream.CanWrite)
                {
                    _networkStream.Write(data, 0, length);
                    _networkStream.Flush();
                    return length;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data: {ex.Message}");
                return -1;
            }
        }

        public void CloseClient()
        {
            try
            {
                if (_networkStream != null)
                    _networkStream.Close();

                if (_tcpClient != null)
                    _tcpClient.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing client: {ex.Message}");
            }
        }
    }

    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public int Length { get; }

        public DataReceivedEventArgs(byte[] data, int length)
        {
            Data = data;
            Length = length;
        }
    }
}
