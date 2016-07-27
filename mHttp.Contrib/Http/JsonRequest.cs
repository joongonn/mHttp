using System;
using System.Collections.Generic;
using System.Net;

using m.Http.Backend;

namespace m.Http
{
    public sealed class JsonRequest<TReq>
    {
        readonly IHttpRequest req;

        public TReq Req { get; private set; }

        public Method Method { get { return req.Method; } }
        public string ContentType { get { return req.ContentType; } }
        public IReadOnlyDictionary<string, string> Headers { get { return req.Headers; } }
        public Uri Url { get { return req.Url; } }

        public string Path { get { return Url.AbsolutePath; } }
        public string Query { get { return Url.Query; } }

        public IReadOnlyDictionary<string, string> PathVariables { get; private set; }

        internal JsonRequest(IHttpRequest req, TReq tReq, IReadOnlyDictionary<string, string> pathVariables)
        {
            this.req = req;
            Req = tReq;
            PathVariables = pathVariables;
        }

        internal static JsonRequest<TReq> From(IHttpRequest req)
        {
            TReq reqObj;
            try
            {
                reqObj = req.Body.FromJson<TReq>();
            }
            catch (Exception e)
            {
                throw new RequestException(string.Format("Error deserializing inputstream to <{0}> - {1}", typeof(TReq).Name, e.Message), e, HttpStatusCode.BadRequest);
            }

            return new JsonRequest<TReq>(req, reqObj, req.PathVariables);
        }
    }
}
