using System;
using System.Net.Sockets;

namespace m.Http.Backend.Tcp
{
    abstract class TcpSessionBase : SessionBase
    {
        internal readonly TcpClient TcpClient;

        protected TcpSessionBase(long id,
                                 TcpClient tcpClient,
                                 int initialReadBufferSize,
                                 int writeTimeoutMs) : base(id, tcpClient.GetStream(), initialReadBufferSize)
        {
            TcpClient = tcpClient;
            TcpClient.NoDelay = true;
            TcpClient.GetStream().WriteTimeout = writeTimeoutMs;
        }

        public bool IsDisconnected()
        {
            return (!TcpClient.Connected) || (TcpClient.Client.Poll(0, SelectMode.SelectRead) && TcpClient.Available == 0);
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            try
            {
                TcpClient.GetStream().Write(buffer, offset, size);
            }
            catch (Exception e)
            {
                throw new SessionStreamException("Exception while writing to stream", e);
            }
        }

        public void CloseQuiety()
        {
            try
            {
                TcpClient.GetStream().Close();
                TcpClient.Close();
            }
            catch
            {
                return;
            }
        }
    }
}
