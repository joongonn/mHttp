using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace m.Http.Backend.Tcp
{
    class Session : SessionBase
    {
        public readonly long Id;
        readonly TcpClient tcpClient;
        readonly int maxKeepAlives;
        readonly TimeSpan readTimeout;

        int start = 0;
        HttpRequest requestState;
        int requests = 0;

        public int KeepAlivesRemaining { get { return maxKeepAlives - requests; } }

        public Session(long id,
                       TcpClient tcpClient,
                       int maxKeepAlives,
                       int initialReadBufferSize,
                       TimeSpan readTimeout,
                       TimeSpan writeTimeout) : base(tcpClient.GetStream(), initialReadBufferSize)
        {
            Id = id;
            this.tcpClient = tcpClient;
            this.maxKeepAlives = maxKeepAlives;
            this.readTimeout = readTimeout;

            this.tcpClient.NoDelay = true;
            this.tcpClient.GetStream().WriteTimeout = (int)writeTimeout.TotalMilliseconds;

            requestState = new HttpRequest();
        }

        public bool TryParseRequestFromBuffer(out IRequest request)
        {
            if (RequestParser.TryParseHttpRequest(buffer, ref start, bufferOffset, requestState, out request))
            {
                start = 0;
                bufferOffset = 0;
                requestState = new HttpRequest();

                requests++;
                return true;
            }
            else
            {
                request = null;
                return false;
            }
        }

        public void WriteResponse(HttpResponse response, bool keepAlive)
        {
            var outputStream = new MemoryStream(1024 + response.Body.Length);

            HttpResponseWriter.WriteResponse(response, outputStream, keepAlive ? KeepAlivesRemaining : 0, readTimeout);

            try
            {
                tcpClient.GetStream().Write(outputStream.GetBuffer(), 0, (int)outputStream.Length);
            }
            catch (Exception e)
            {
                throw new SessionStreamException("Exception while writing to stream", e);
            }
        }

        public bool IsDisconnected()
        {
            return (!tcpClient.Connected) || (tcpClient.Client.Poll(0, SelectMode.SelectRead) && tcpClient.Available == 0);
        }

        public void Close()
        {
            try
            {
                tcpClient.GetStream().Close();
                tcpClient.Close();
            }
            catch
            {
                return;
            }
        }
    }
}
