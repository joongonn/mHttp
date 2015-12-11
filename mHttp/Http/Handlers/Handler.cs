using System;
using System.Net;
using System.Threading.Tasks;

namespace m.Http.Handlers
{
    public static class Handler
    {
        static readonly HttpResponse EmptyResponse = new HttpResponse(HttpStatusCode.NoContent, ContentTypes.Html);
        static readonly Task<HttpResponse> EmptyResponseTask = Task.FromResult(EmptyResponse);

        public static Func<IHttpRequest, Task<HttpResponse>> FromAction(Action a)
        {
            return (IHttpRequest _) =>
            {
                a();
                return EmptyResponseTask;
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> FromAsyncAction(Func<Task> f)
        {
            return (IHttpRequest _) =>
            {
                f();
                return EmptyResponseTask;
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> FromAction(Action<IHttpRequest> a)
        {
            return (IHttpRequest req) =>
            {
                a(req);
                return EmptyResponseTask;
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> FromAsyncAction(Func<IHttpRequest, Task> a)
        {
            return (IHttpRequest req) =>
            {
                a(req);
                return EmptyResponseTask;
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> From(Func<HttpResponse> f)
        {
            return (IHttpRequest _) =>
            {
                HttpResponse resp = f();
                return Task.FromResult(resp);
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> From(Func<IHttpRequest, HttpResponse> f)
        {
            return (IHttpRequest req) =>
            {
                HttpResponse resp = f(req);
                return Task.FromResult(resp);
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> FromAsync(Func<Task<HttpResponse>> f)
        {
            return (IHttpRequest _) => f();
        }
    }
}
