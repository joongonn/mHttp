﻿using System;
using System.Collections.Generic;
using System.Net;

using m.Utils;

namespace m.Http
{
    public sealed class JsonRequest<TReq> : IMatchedRequest
    {
        readonly IMatchedRequest req;

        public TReq Req { get; private set; }

        public Method Method { get { return req.Method; } }
        public string ContentType { get { return req.ContentType; } }
        public IReadOnlyDictionary<string, string> Headers { get { return req.Headers; } }
        public Uri Url { get { return req.Url; } }

        public string Path { get { return Url.AbsolutePath; } }
        public string Query { get { return Url.Query; } }

        public IReadOnlyDictionary<string, string> UrlVariables { get; private set; }

        internal JsonRequest(IMatchedRequest req, TReq tReq, IReadOnlyDictionary<string, string> urlVariables)
        {
            this.req = req;
            Req = tReq;
            UrlVariables = urlVariables;
        }

        internal static JsonRequest<TReq> From(Request req)
        {
            TReq reqObj;
            try
            {
                reqObj = req.InputStream.FromJson<TReq>();
            }
            catch (Exception e)
            {
                throw new RequestException(string.Format("Error deserializing inputstream to <{0}> - {1}", typeof(TReq).Name, e.Message), e, HttpStatusCode.BadRequest);
            }

            return new JsonRequest<TReq>(req, reqObj, req.UrlVariables);
        }
    }
}
