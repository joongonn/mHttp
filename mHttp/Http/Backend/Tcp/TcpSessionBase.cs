using System.IO;
using System.Net.Sockets;

namespace m.Http.Backend.Tcp
{
    abstract class TcpSessionBase : SessionBase
    {
        internal TcpClient TcpClient { get; }

        protected TcpSessionBase(long id,
                                 TcpClient tcpClient,
                                 Stream stream,
                                 int initialReadBufferSize,
                                 int readTimeoutMs,
                                 int writeTimeoutMs) : base(id, stream, initialReadBufferSize)
        {
            TcpClient = tcpClient;
            TcpClient.NoDelay = true;
            stream.ReadTimeout = readTimeoutMs; // Does NOT take effect in asyncs
            stream.WriteTimeout = writeTimeoutMs;
        }

        public bool IsDisconnected()
        {
            return (!TcpClient.Connected) || (TcpClient.Client.Poll(0, SelectMode.SelectRead) && TcpClient.Available == 0);
        }

        public void CloseQuiety()
        {
            try
            {
                Stream.Close();
                TcpClient.Close();
            }
            catch
            {
                return;
            }
        }
    }
}
