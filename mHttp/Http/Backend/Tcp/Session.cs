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
        int currentRequestBytes = 0;
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

        public bool TryParseNextRequestFromBuffer(out int requestBytes, out HttpRequest request)
        {
            if (dataStart == readBufferOffset)
            {
                requestBytes = -1;
                request = null;
                return false;
            }

            if (requestState == null)
            {
                currentRequestBytes = 0;
                requestState = new HttpRequest();
            }

            var initialDataStart = dataStart;
            var isRequestComplete = RequestParser.TryParseHttpRequest(readBuffer, ref dataStart, readBufferOffset, requestState, out request);
            currentRequestBytes += dataStart - initialDataStart;

            if (isRequestComplete)
            {
                requestBytes = currentRequestBytes;
                CompactReadBuffer(ref dataStart);
                currentRequestBytes = 0;
                requestState = null;
                requests++;
                return true;
            }
            else
            {
                requestBytes = -1;
                request = null;
                return false;
            }
        }

        public int WriteResponse(HttpResponse response, bool keepAlive)
        {
            using (var outputBuffer = new MemoryStream(512 + response.Body.Length))
            {
                int bytesToWrite = HttpResponseWriter.Write(response, outputBuffer, keepAlive ? KeepAlivesRemaining : 0, readTimeout);
                Write(outputBuffer.GetBuffer(), 0, bytesToWrite);

                return bytesToWrite;
            }
        }

        public int WriteWebSocketUpgradeResponse(WebSocketUpgradeResponse response)
        {
            using (var outputBuffer = new MemoryStream(512))
            {
                int bytesWritten = HttpResponseWriter.WriteWebSocketUpgradeResponse(response, outputBuffer);
                Write(outputBuffer.GetBuffer(), 0, bytesWritten);

                return bytesWritten;
            }
        }
    }
}
