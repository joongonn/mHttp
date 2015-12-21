using System;
using System.Net;
using System.Threading.Tasks;

using m.Http.Backend;

namespace m.Http
{
    using Handler = Func<IHttpRequest, Task<HttpResponse>>;

    public static class Handlers
    {
        static readonly Task<HttpResponse> EmptyResponse = Task.FromResult(new HttpResponse(HttpStatusCode.NoContent));

        public static Handler FromAction(Action a)
        {
            return (IHttpRequest _) =>
            {
                a();
                return EmptyResponse;
            };
        }

        public static Handler FromAsyncAction(Func<Task> f)
        {
            return (IHttpRequest _) =>
            {
                f();
                return EmptyResponse;
            };
        }

        public static Handler FromAction(Action<IHttpRequest> a)
        {
            return (IHttpRequest req) =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static Handler FromAsyncAction(Func<IHttpRequest, Task> a)
        {
            return (IHttpRequest req) =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static Handler From(Func<HttpResponse> f)
        {
            return (IHttpRequest _) =>
            {
                HttpResponse resp = f();
                return Task.FromResult(resp);
            };
        }

        public static Handler From(Func<IHttpRequest, HttpResponse> f)
        {
            return (IHttpRequest req) =>
            {
                HttpResponse resp = f(req);
                return Task.FromResult(resp);
            };
        }

        public static Handler FromAsync(Func<Task<HttpResponse>> f)
        {
            return (IHttpRequest _) => f();
        }

        #region Websocket
        public static Handler From(Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f)
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
