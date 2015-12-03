using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace m.Http.Handlers
{
    class EmptyResponse : HttpResponse
    {
        public static readonly HttpResponse Instance = new EmptyResponse();

        EmptyResponse() : base(HttpStatusCode.NoContent, ContentTypes.Html) { }
    }

    public static class Handler
    {
        public static Func<Request, Task<HttpResponse>> FromAction(Action a)
        {
            return (Request _) =>
            {
                a();
                return Task.FromResult(EmptyResponse.Instance);
            };
        }

        public static Func<Request, Task<HttpResponse>> FromAsyncAction(Func<Task> f)
        {
            return async (Request _) =>
            {
                await f();
                return EmptyResponse.Instance;
            };
        }

        public static Func<Request, Task<HttpResponse>> FromAction(Action<Request> a)
        {
            return (Request req) =>
            {
                a(req);
                return Task.FromResult(EmptyResponse.Instance);
            };
        }

        public static Func<Request, Task<HttpResponse>> FromAsyncAction(Func<Request, Task> f)
        {
            return async (Request req) =>
            {
                await f(req);
                return EmptyResponse.Instance;
            };
        }

        public static Func<Request, Task<HttpResponse>> From(Func<HttpResponse> f)
        {
            return (Request _) =>
            {
                HttpResponse resp = f();
                return Task.FromResult(resp);
            };
        }

        public static Func<Request, Task<HttpResponse>> From(Func<Request, HttpResponse> f)
        {
            return (Request req) =>
            {
                HttpResponse resp = f(req);
                return Task.FromResult(resp);
            };
        }
        public static Func<Request, Task<HttpResponse>> FromAsync(Func<Task<HttpResponse>> f)
        {
            return async (Request _) =>
            {
                HttpResponse resp = await f();
                return resp;
            };
        }
    }
}
