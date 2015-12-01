using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace m.Http.Backend.Tcp
{
    class Session
    {
        public readonly long Id;
        readonly TcpClient tcpClient;
        public readonly NetworkStream stream;

        public Session(long id, TcpClient tcpClient, TimeSpan writeTimeout)
        {
            Id = id;
            this.tcpClient = tcpClient;
            this.tcpClient.NoDelay = true;
            stream = this.tcpClient.GetStream();
            stream.WriteTimeout = (int)writeTimeout.TotalMilliseconds;
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            return stream.ReadAsync(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            stream.Write(buffer, offset, size);
        }

        public bool IsDisconnected()
        {
            return (!tcpClient.Connected) || (tcpClient.Client.Poll(0, SelectMode.SelectRead) && tcpClient.Available == 0);
        }

        public void Close()
        {
            try
            {
                stream.Close();
                tcpClient.Close();
            }
            catch
            {
                return;
            }
        }
    }
}
