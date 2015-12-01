using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace m.Http.Handlers
{
    class EmptyResponse : IHttpResponse
    {
        public static readonly IHttpResponse Instance = new EmptyResponse();

        readonly byte[] Empty = new byte[0];
        readonly IDictionary<string, string> EmptyHeaders = new Dictionary<string, string>(0);

        public string ContentType { get { return ContentTypes.Html; } }
        public HttpStatusCode StatusCode { get { return HttpStatusCode.NoContent; } }
        public string StatusDescription { get { return StatusCode.ToString(); } }
        public IDictionary<string, string> Headers { get { return EmptyHeaders; } }
        public byte[] Body { get { return Empty; } }

        EmptyResponse() { }
    }

    public static class Handler
    {
        public static Func<Request, Task<IHttpResponse>> FromAction(Action a)
        {
            return (Request _) =>
            {
                a();
                return Task.FromResult(EmptyResponse.Instance);
            };
        }

        public static Func<Request, Task<IHttpResponse>> FromAsyncAction(Func<Task> f)
        {
            return async (Request _) =>
            {
                await f();
                return EmptyResponse.Instance;
            };
        }

        public static Func<Request, Task<IHttpResponse>> FromAction(Action<Request> a)
        {
            return (Request req) =>
            {
                a(req);
                return Task.FromResult(EmptyResponse.Instance);
            };
        }

        public static Func<Request, Task<IHttpResponse>> FromAsyncAction(Func<Request, Task> f)
        {
            return async (Request req) =>
            {
                await f(req);
                return EmptyResponse.Instance;
            };
        }

        public static Func<Request, Task<IHttpResponse>> From(Func<IHttpResponse> f)
        {
            return (Request _) =>
            {
                IHttpResponse resp = f();
                return Task.FromResult(resp);
            };
        }

        public static Func<Request, Task<IHttpResponse>> From(Func<Request, IHttpResponse> f)
        {
            return (Request req) =>
            {
                IHttpResponse resp = f(req);
                return Task.FromResult(resp);
            };
        }
        public static Func<Request, Task<IHttpResponse>> FromAsync(Func<Task<IHttpResponse>> f)
        {
            return async (Request _) =>
            {
                IHttpResponse resp = await f();
                return resp;
            };
        }
    }
}

