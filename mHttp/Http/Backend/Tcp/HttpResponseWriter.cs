using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace m.Http.Backend.Tcp
{
    static class HttpResponseWriter
    {
        static readonly string Server = string.Format("mHttp {0}", Assembly.GetExecutingAssembly().GetName().Version);

        public static byte[] GetStatusAndHeaders(int statusCode,
                                                 string statusDescription,
                                                 string contentType,
                                                 long contentLength,
                                                 int keepAlives,
                                                 TimeSpan keepAliveTimeout,
                                                 IDictionary<String, String> headers)
        {
            var sb = new StringBuilder(512);

            sb.Append("HTTP/1.1 ").Append(statusCode).Append(" ").Append(statusDescription).Append("\r\n");

            sb.Append("Server: ").Append(Server).Append("\r\n");
            sb.Append("Date: ").Append(DateTime.UtcNow.ToString("R")).Append("\r\n");
            if (!string.IsNullOrEmpty(contentType))
            {
                sb.Append("Content-Type: ").Append(contentType).Append("\r\n");
            }
            sb.Append("Content-Length: ").Append(contentLength).Append("\r\n");

            if (headers?.Count > 0)
            {
                foreach (var kvp in headers) //TODO: duplicate handling with fixed headers
                {
                    sb.Append(kvp.Key).Append(": ").Append(kvp.Value).Append("\r\n");
                }
            }

            if (keepAlives > 0)
            {
                sb.Append("Connection: keep-alive\r\n");
                sb.Append("Keep-Alive: timeout=").Append((int)keepAliveTimeout.TotalSeconds).Append(",max=").Append("keepAlives").Append("\r\n");
            }
            else
            {
                sb.Append("Connection: close\r\n");
            }

            sb.Append("\r\n");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        public static byte[] GetAcceptWebSocketUpgradeResponse(int statusCode, string statusDescription, string requestKey)
        {
            var sb = new StringBuilder(512);

            sb.Append("HTTP/1.1 ").Append(statusCode).Append(" ").Append(statusDescription).Append("\r\n");

            sb.Append("Server: ").Append(Server).Append("\r\n");
            sb.Append("Date: ").Append(DateTime.UtcNow.ToString("R")).Append("\r\n");

            var acceptKey = Encoding.ASCII.GetBytes(requestKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
            var acceptKeyHash = Convert.ToBase64String(SHA1.Create().ComputeHash(acceptKey));

            sb.Append("Connection: upgrade\r\n");
            sb.Append("Upgrade: websocket\r\n");
            sb.Append("Sec-WebSocket-Accept: ").Append(acceptKeyHash).Append("\r\n");

            sb.Append("\r\n");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        public static byte[] GetRejectWebSocketUpgradeResponse(int statusCode, string statusDescription)
        {
            var sb = new StringBuilder(512);

            sb.Append("HTTP/1.1 ").Append(statusCode).Append(" ").Append(statusDescription).Append("\r\n");

            sb.Append("Server: ").Append(Server).Append("\r\n");
            sb.Append("Date: ").Append(DateTime.UtcNow.ToString("R")).Append("\r\n");
            sb.Append("Connection: close\r\n");

            sb.Append("\r\n");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}
