using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using m.Utils;

namespace m.Http.Backend.Tcp
{
    //TODO: consider avoiding underlying string.Format by going calling WriteAscii() multiple times for each header instead
    static class HttpResponseWriter
    {
        static readonly byte[] CRLF = { 13, 10 };
        static readonly string Server = string.Format("mHttp {0}", Assembly.GetExecutingAssembly().GetName().Version);

        public static int Write(HttpResponse response, MemoryStream ms, int keepAlives, TimeSpan keepAliveTimeout)
        {
            var bytesWritten = 0;

            bytesWritten += ms.WriteAsciiFormat("HTTP/1.1 {0} {1}\r\n", (int)response.StatusCode, response.StatusDescription);
            bytesWritten += ms.WriteAsciiFormat("Server: {0}\r\n", Server);
            bytesWritten += ms.WriteAsciiFormat("Date: {0}\r\n", DateTime.UtcNow.ToString("R"));

            if (!string.IsNullOrEmpty(response.ContentType))
            {
                bytesWritten += ms.WriteAsciiFormat("Content-Type: {0}\r\n", response.ContentType);
            }
            var contentLength = response.Body == null ? 0 : response.Body.Length;
            bytesWritten += ms.WriteAsciiFormat("Content-Length: {0}\r\n", contentLength);

            var headers = response.Headers;
            if (headers?.Count > 0)
            {
                foreach (var kvp in headers) //TODO: duplicate handling with fixed headers
                {
                    bytesWritten += ms.WriteAsciiFormat("{0}: {1}\r\n", kvp.Key, kvp.Value);
                }
            }

            if (keepAlives > 0)
            {
                bytesWritten += ms.WriteAscii("Connection: keep-alive\r\n");
                bytesWritten += ms.WriteAsciiFormat("Keep-Alive: timeout={0},max={1}\r\n", (int)keepAliveTimeout.TotalSeconds, keepAlives);
            }
            else
            {
                bytesWritten += ms.WriteAscii("Connection: close\r\n");
            }

            bytesWritten += ms.Write(CRLF);

            if (contentLength > 0)
            {
                bytesWritten += ms.Write(response.Body);
            }

            return bytesWritten;
        }

        public static int WriteWebSocketUpgradeResponse(WebSocketUpgradeResponse response, MemoryStream ms)
        {
            var bytesWritten = 0;

            bytesWritten += ms.WriteAsciiFormat("HTTP/1.1 {0} {1}\r\n", (int)response.StatusCode, response.StatusDescription);
            bytesWritten += ms.WriteAsciiFormat("Server: {0}\r\n", Server);
            bytesWritten += ms.WriteAsciiFormat("Date: {0}\r\n", DateTime.UtcNow.ToString("R"));

            var acceptResponse = response as WebSocketUpgradeResponse.AcceptUpgradeResponse;
            if (acceptResponse != null)
            {
                var acceptKey = Encoding.UTF8.GetBytes(acceptResponse.RequestKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
                var acceptKeyHash = Convert.ToBase64String(SHA1.Create().ComputeHash(acceptKey));

                bytesWritten += ms.WriteAscii("Connection: upgrade\r\n");
                bytesWritten += ms.WriteAscii("Upgrade: websocket\r\n");
                bytesWritten += ms.WriteAsciiFormat("Sec-WebSocket-Accept: {0}\r\n", acceptKeyHash);
            }
            else // Reject
            {
                bytesWritten += ms.WriteAscii("Connection: close\r\n");
            }

            bytesWritten += ms.Write(CRLF);

            return bytesWritten;
        }
    }
}
