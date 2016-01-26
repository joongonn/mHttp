using System;
using System.IO;
using System.Net.Sockets;

namespace m.Http.Backend.Tcp
{
    abstract class TcpSessionBase : SessionBase
    {
        internal TcpClient TcpClient { get; }
        internal Stream Stream { get; }

        protected TcpSessionBase(long id,
                                 TcpClient tcpClient,
                                 Stream stream,
                                 int initialReadBufferSize,
                                 int writeTimeoutMs) : base(id, stream, initialReadBufferSize)
        {
            TcpClient = tcpClient;
            TcpClient.NoDelay = true;
            Stream = stream;
            Stream.WriteTimeout = writeTimeoutMs;
        }

        public bool IsDisconnected()
        {
            return (!TcpClient.Connected) || (TcpClient.Client.Poll(0, SelectMode.SelectRead) && TcpClient.Available == 0);
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            try
            {
                Stream.Write(buffer, offset, size);
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
