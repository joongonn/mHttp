using System;
using System.Collections.Generic;
using System.IO;

namespace m.Http
{
    public class Request : IMatchedRequest
    {
        readonly HttpRequest httpReq;

        public Method Method { get { return httpReq.Method; } }
        public string ContentType { get { return httpReq.ContentType; } }
        public IReadOnlyDictionary<string, string> Headers { get { return httpReq.Headers; } }
        public Uri Url { get { return httpReq.Url; } }

        public string Path { get { return Url.AbsolutePath; } }
        public string Query { get { return Url.Query; } }

        public IReadOnlyDictionary<string, string> UrlVariables { get; private set; }

        public Stream InputStream { get { return httpReq.InputStream; } }

        internal Request(HttpRequest httpReq, IReadOnlyDictionary<string, string> urlVariables)
        {
            this.httpReq = httpReq;
            UrlVariables = urlVariables;
        }
    }
}
