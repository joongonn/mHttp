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

        public IReadOnlyDictionary<string, string> PathVariables { get; private set; }

        public Stream Body { get { return httpReq.Body; } }

        public Request(HttpRequest httpReq, IReadOnlyDictionary<string, string> pathVariables)
        {
            this.httpReq = httpReq;
            PathVariables = pathVariables;
        }
    }
}
