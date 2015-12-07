using System;
using System.IO;

using m.Utils;

namespace m.Http.Backend.Tcp
{
    static class HttpResponseWriter
    {
        const string ResponsePrefixTemplate = "HTTP/1.1 {0} {1}\r\n" +
                                              "Content-Type: {2}\r\n" +
                                              "Server: {3}\r\n" +
                                              "Date: {4}\r\n" +
                                              "Content-Length: {5}\r\n" +
                                              "Connection: {6}\r\n";

        static readonly byte[] CRLF = new byte[] { 13, 10 };
        static readonly string Server = string.Format("mHttp {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

        public static void WriteResponse(HttpResponse httpResponse, Stream buffer, int keepAlives, TimeSpan keepAliveTimeout)
        {
            var contentLength = (httpResponse.Body == null) ? 0 : httpResponse.Body.Length;

            var responsePrefix = string.Format(ResponsePrefixTemplate,
                                               (int)httpResponse.StatusCode, httpResponse.StatusCode,
                                               httpResponse.ContentType,
                                               Server,
                                               DateTime.UtcNow.ToString("r"),
                                               contentLength,
                                               keepAlives > 0 ? "keep-alive" : "close");

            buffer.WriteAscii(responsePrefix);

            if (keepAlives > 0)
            {
                buffer.WriteAscii(string.Format("Keep-Alive: timeout={0},max={1}\r\n", (int)keepAliveTimeout.TotalSeconds, keepAlives));
            }

            if (httpResponse.Headers != null && httpResponse.Headers.Count > 0)
            {
                foreach (var kvp in httpResponse.Headers) //TODO: duplicate handling with above
                {
                    buffer.WriteAscii(string.Format("{0}: {1}\r\n", kvp.Key, kvp.Value));
                }
            }

            buffer.Write(CRLF, 0, CRLF.Length);

            // Body
            if (contentLength > 0)
            {
                buffer.Write(httpResponse.Body, 0, contentLength);
            }
        }
    }
}

