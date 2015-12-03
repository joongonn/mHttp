using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using m.Utils;

namespace m.Http
{
    sealed class ErrorResponse : HttpResponse
    {
        public readonly Exception Exception;

        public ErrorResponse(HttpStatusCode statusCode) : base(statusCode, ContentTypes.Html)
        {
            Exception = null;
        }

        public ErrorResponse(HttpStatusCode statusCode, string statusDescription) : base(statusCode, statusDescription, ContentTypes.Html)
        {
            Exception = null;
        }

        public ErrorResponse(HttpStatusCode statusCode, Exception exception) : base(statusCode, ContentTypes.Html)
        {
            Exception = exception;
        }
    }

    public sealed class TextResponse : HttpResponse
    {
        public TextResponse(string text) : base(HttpStatusCode.OK, ContentTypes.Plain, Encoding.UTF8.GetBytes(text)) { }
    }

    public sealed class JsonResponse : HttpResponse
    {
        public JsonResponse(string json) : base(HttpStatusCode.OK, ContentTypes.Json, Encoding.UTF8.GetBytes(json)) { }

        public JsonResponse(object t) : this(t.ToJson()) { }
    }

    public abstract class HttpResponse
    {
        static readonly byte[] Empty = new byte[0];

        public HttpStatusCode StatusCode { get; protected set; }
        public string StatusDescription { get; protected set; }
        public string ContentType { get; protected set; }
        public IDictionary<string, string> Headers { get; protected set; }

        public byte[] Body { get; protected set; }

        protected HttpResponse(HttpStatusCode statusCode, string contentType) : this(statusCode, contentType, Empty) { }

        protected HttpResponse(HttpStatusCode statusCode, string contentType, byte[] body) : this(statusCode, statusCode.ToString(), contentType, new Dictionary<string, string>(), body) { }

        protected HttpResponse(HttpStatusCode statusCode, string statusDescription, string contentType) : this(statusCode, statusDescription, contentType, new Dictionary<string, string>(), Empty) { }

        protected HttpResponse(HttpStatusCode statusCode, string statusDescription, string contentType, IDictionary<string, string> headers, byte[] body)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            ContentType = contentType;
            Headers = headers;
            Body = body;
        }

        public static implicit operator HttpResponse(string text)
        {
            return new TextResponse(text);
        }
    }
}
