using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

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

    public class HttpResponse
    {
        protected static readonly Dictionary<string, string> EmptyHeaders = new Dictionary<string, string>(0);
        protected static readonly byte[] EmptyBody = new byte[0];

        public HttpStatusCode StatusCode { get; private set; }
        public string StatusDescription { get; private set; }
        public string ContentType { get; private set; }
        public IDictionary<string, string> Headers { get; private set; }
        public byte[] Body { get; private set; }

        public HttpResponse(HttpStatusCode statusCode) : this(statusCode, ContentTypes.Html) { }

        public HttpResponse(HttpStatusCode statusCode, string contentType) : this(statusCode, contentType, EmptyBody) { }

        public HttpResponse(HttpStatusCode statusCode, string contentType, byte[] body) : this(statusCode, statusCode.ToString(), contentType, EmptyHeaders, body) { }

        protected HttpResponse(HttpStatusCode statusCode, string statusDescription, string contentType) : this(statusCode, statusDescription, contentType, EmptyHeaders, EmptyBody) { }

        HttpResponse(HttpStatusCode statusCode, string statusDescription, string contentType, IDictionary<string, string> headers, byte[] body)
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
