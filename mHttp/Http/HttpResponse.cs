using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using m.Utils;

namespace m.Http
{
    public static class HttpResponse
    {
        public static readonly IHttpResponse NotFound = Error(HttpStatusCode.NotFound);
        public static readonly IHttpResponse ServiceUnavailable = Error(HttpStatusCode.ServiceUnavailable);

        public abstract class Response : IHttpResponse
        {
            static readonly byte[] Empty = new byte[0];

            public string ContentType { get; protected set; }
            public HttpStatusCode StatusCode { get; protected set; }
            public string StatusDescription { get; protected set; }
            public IDictionary<string, string> Headers { get; protected set; }
            public byte[] Body { get; protected set; }

            protected Response(string contentType, HttpStatusCode statusCode) : this(contentType, statusCode, Empty) { }

            protected Response(string contentType, HttpStatusCode statusCode, byte[] body) : this(contentType, statusCode, statusCode.ToString(), new Dictionary<string, string>(), body) { }

            protected Response(string contentType, HttpStatusCode statusCode, string statusDescription) : this(contentType, statusCode, statusDescription, new Dictionary<string, string>(), Empty) { }

            protected Response(string contentType, HttpStatusCode statusCode, string statusDescription, IDictionary<string, string> headers, byte[] body)
            {
                ContentType = contentType;
                StatusCode = statusCode;
                StatusDescription = statusDescription;
                Headers = headers;
                Body = body;
            }
        }

        sealed class ErrorResponse : Response
        {
            public readonly Exception Exception;

            public ErrorResponse(HttpStatusCode statusCode) : base(ContentTypes.Html, statusCode)
            {
                Exception = null;
            }

            public ErrorResponse(HttpStatusCode statusCode, string statusDescription) : base(ContentTypes.Html, statusCode, statusDescription)
            {
                Exception = null;
            }

            public ErrorResponse(HttpStatusCode statusCode, Exception exception) : base(ContentTypes.Html, statusCode)
            {
                Exception = exception;
            }
        }

        public sealed class TextResponse : Response
        {
            public TextResponse(string text) : base(ContentTypes.Plain, HttpStatusCode.OK, Encoding.UTF8.GetBytes(text)) { }
        }

        public sealed class JsonResponse : Response
        {
            public JsonResponse(string json) : base(ContentTypes.Json, HttpStatusCode.OK, Encoding.UTF8.GetBytes(json)) { }

            public JsonResponse(object t) : this(t.ToJson()) { }
        }

        public static IHttpResponse Error(HttpStatusCode statusCode)
        {
            return new ErrorResponse(statusCode);
        }
        public static IHttpResponse Error(HttpStatusCode statusCode, string statusDescription)
        {
            return new ErrorResponse(statusCode, statusDescription);
        }
        public static IHttpResponse Error(HttpStatusCode statusCode, Exception exception)
        {
            return new ErrorResponse(statusCode, exception);
        }

        public static IHttpResponse Text(string text)
        {
            return new TextResponse(text);
        }

        public static IHttpResponse Json(object o)
        {
            return new JsonResponse(o);
        }

        public static IHttpResponse GZip(IHttpResponse resp)
        {
            throw new NotImplementedException(); //FIXME:
        }
    }
}
