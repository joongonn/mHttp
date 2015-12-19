using System;
using System.IO;
using System.Net.Sockets;

using m.Http;

namespace m.Http.Backend.Tcp
{
    class Session : TcpSessionBase
    {
        readonly int maxKeepAlives;
        readonly TimeSpan readTimeout;

        int dataStart = 0;
        HttpRequest requestState;
        int requests = 0;

        public int KeepAlivesRemaining { get { return maxKeepAlives - requests; } }

        public Session(long id,
                       TcpClient tcpClient,
                       int maxKeepAlives,
                       int initialReadBufferSize,
                       TimeSpan readTimeout,
                       TimeSpan writeTimeout) : base(id, tcpClient, initialReadBufferSize, (int)writeTimeout.TotalMilliseconds)
        {
            this.maxKeepAlives = maxKeepAlives;
            this.readTimeout = readTimeout;

            requestState = null;
        }

        public bool TryParseNextRequestFromBuffer(out HttpRequest request)
        {
            if (dataStart == readBufferOffset)
            {
                request = null;
                return false;
            }

            if (requestState == null)
            {
                requestState = new HttpRequest();
            }
            
            if (RequestParser.TryParseHttpRequest(readBuffer, ref dataStart, readBufferOffset, requestState, out request))
            {
                CompactReadBuffer(ref dataStart);
                requestState = null;
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
            using (var outputBuffer = new MemoryStream(512 + response.Body.Length))
            {
                HttpResponseWriter.Write(response, outputBuffer, keepAlive ? KeepAlivesRemaining : 0, readTimeout);

                Write(outputBuffer.GetBuffer(), 0, (int)outputBuffer.Length);
            }
        }

        public void WriteWebSocketUpgradeResponse(WebSocketUpgradeResponse response)
        {
            using (var outputBuffer = new MemoryStream(512))
            {
                HttpResponseWriter.WriteWebSocketUpgradeResponse(response, outputBuffer);

                Write(outputBuffer.GetBuffer(), 0, (int)outputBuffer.Length);
            }
        }

        public override void Dispose()
        {
            CloseQuiety();
        }
    }
}
