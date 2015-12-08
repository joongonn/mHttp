using System;
using System.Net;
using System.Threading.Tasks;

namespace m.Http.Handlers
{
    public static class Handler
    {
        static readonly HttpResponse EmptyResponse = new HttpResponse(HttpStatusCode.NoContent, ContentTypes.Html);
        static readonly Task<HttpResponse> EmptyResponseTask = Task.FromResult(EmptyResponse);

        public static Func<Request, Task<HttpResponse>> FromAction(Action a)
        {
            return (Request _) =>
            {
                a();
                return EmptyResponseTask;
            };
        }

        public static Func<Request, Task<HttpResponse>> FromAsyncAction(Func<Task> f)
        {
            return (Request _) =>
            {
                f();
                return EmptyResponseTask;
            };
        }

        public static Func<Request, Task<HttpResponse>> FromAction(Action<Request> a)
        {
            return (Request req) =>
            {
                a(req);
                return EmptyResponseTask;
            };
        }

        public static Func<Request, Task<HttpResponse>> FromAsyncAction(Func<Request, Task> a)
        {
            return (Request req) =>
            {
                a(req);
                return EmptyResponseTask;
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
            return (Request _) => f();
        }
    }
}
