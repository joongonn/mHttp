using System;
using System.Net;
using System.Threading.Tasks;

using m.Http.Backend;
using m.Http.Handlers;

namespace m.Http
{
    using SyncHandler = Func<IHttpRequest, HttpResponse>;
    using AsyncHandler = Func<IHttpRequest, Task<HttpResponse>>;

    public static class Handler
    {
        static readonly Task<HttpResponse> EmptyResponse = Task.FromResult(new HttpResponse(HttpStatusCode.NoContent));

        public static AsyncHandler FromAction(Action a)
        {
            return _ =>
            {
                a();
                return EmptyResponse;
            };
        }

        public static AsyncHandler FromAsyncAction(Func<Task> f)
        {
            return _ =>
            {
                f();
                return EmptyResponse;
            };
        }

        public static AsyncHandler FromAction(Action<IHttpRequest> a)
        {
            return req =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static AsyncHandler FromAsyncAction(Func<IHttpRequest, Task> a)
        {
            return req =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static AsyncHandler From(Func<HttpResponse> f)
        {
            return _ =>
            {
                HttpResponse resp = f();
                return Task.FromResult(resp);
            };
        }

        public static AsyncHandler From(Func<IHttpRequest, HttpResponse> f)
        {
            return req =>
            {
                HttpResponse resp = f(req);
                return Task.FromResult(resp);
            };
        }

        public static AsyncHandler FromAsync(Func<Task<HttpResponse>> f)
        {
            return _ => f();
        }

        public static AsyncHandler From(Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f)
        {
            return req =>
            {
                var httpRequest = (HttpRequest)req;

                HttpResponse resp = f((IWebSocketUpgradeRequest)httpRequest);
                return Task.FromResult(resp);
            };
        }
    }
}
