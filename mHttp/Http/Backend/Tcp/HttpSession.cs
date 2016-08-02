using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace m.Http.Backend.Tcp
{
    class HttpSession : TcpSessionBase
    {
        readonly bool isSecured;
        readonly int maxKeepAlives;

        int dataStart = 0;
        int currentRequestBytes = 0;
        HttpRequest requestState;
        int requests = 0;

        public int KeepAlivesRemaining { get { return maxKeepAlives - requests; } }

        public HttpSession(long id,
                           TcpClient tcpClient,
                           Stream stream,
                           bool isSecured,
                           int maxKeepAlives,
                           int initialReadBufferSize,
                           int readTimeoutMs,
                           int writeTimeoutMs) : base(id, tcpClient, stream, initialReadBufferSize, readTimeoutMs, writeTimeoutMs)
        {
            this.isSecured = isSecured;
            this.maxKeepAlives = maxKeepAlives;

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
                requestState = new HttpRequest((IPEndPoint)TcpClient.Client.RemoteEndPoint, isSecured);
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
                if (requestState.State == RequestParser.State.ReadBodyToEnd && readBuffer.Length >= 32768)
                {
                    CompactReadBuffer(ref dataStart); // read in block of max. 32kb, do not allow further expansion of buffer (eg. big file post upload)
                }

                requestBytes = -1;
                request = null;
                return false;
            }
        }
    }
}
