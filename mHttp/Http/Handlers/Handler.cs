using System;
using System.Net;
using System.Threading.Tasks;

using m.Http.Backend;

namespace m.Http.Handlers
{
    public static class Handler
    {
        static readonly Task<HttpResponse> EmptyResponse = Task.FromResult(new HttpResponse(HttpStatusCode.NoContent));

        public static Func<IHttpRequest, Task<HttpResponse>> FromAction(Action a)
        {
            return (IHttpRequest _) =>
            {
                a();
                return EmptyResponse;
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> FromAsyncAction(Func<Task> f)
        {
            return (IHttpRequest _) =>
            {
                f();
                return EmptyResponse;
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> FromAction(Action<IHttpRequest> a)
        {
            return (IHttpRequest req) =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> FromAsyncAction(Func<IHttpRequest, Task> a)
        {
            return (IHttpRequest req) =>
            {
                a(req);
                return EmptyResponse;
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

        #region Websocket
        public static Func<IHttpRequest, Task<HttpResponse>> From(Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f)
        {
            return (IHttpRequest req) =>
            {
                var httpRequest = (HttpRequest)req;

                HttpResponse resp = f((IWebSocketUpgradeRequest)httpRequest);
                return Task.FromResult(resp);
            };
        }
        #endregion
    }
}
