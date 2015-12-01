using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;

namespace m.Http
{
    public sealed class HttpRequest : IHttpRequest
    {
        public Method Method { get; private set; }
        public string ContentType { get; private set; }
        public IReadOnlyDictionary<string, string> Headers  { get; private set; }
        public Uri Url { get; private set; }

        public string Path { get { return Url.AbsolutePath; } }
        public string Query { get { return Url.Query; } }

        public bool IsKeepAlive { get; private set; }

        public Stream InputStream { get; private set; }

        public HttpRequest(Method method,
                           string contentType,
                           IReadOnlyDictionary<string, string> headers,
                           Uri url,
                           bool isKeepAlive,
                           Stream inputStream)
        {
            Method = method;
            ContentType = contentType;
            Url = url;
            IsKeepAlive = isKeepAlive;
            Headers = headers;
            InputStream = inputStream;
        }

        public static implicit operator HttpRequest(HttpListenerRequest req)
        {
            return new HttpRequest(req.GetMethod(),
                                   req.ContentType,
                                   req.Headers.AllKeys.ToDictionary(k => k, k => req.Headers[k]),
                                   req.Url,
                                   req.KeepAlive,
                                   req.InputStream);
        }
    }
}
