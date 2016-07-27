using System;
using System.Net;
using System.Threading.Tasks;

using m.Http.Backend;

namespace m.Http.Handlers
{
    using AsyncRequestHandler = Func<IHttpRequest, Task<HttpResponse>>;

    static class Handler
    {
        static readonly Task<HttpResponse> EmptyResponse = Task.FromResult(new HttpResponse(HttpStatusCode.NoContent));

        public static AsyncRequestHandler FromAction(Action a)
        {
            return _ =>
            {
                a();
                return EmptyResponse;
            };
        }

        public static AsyncRequestHandler FromAsyncAction(Func<Task> f)
        {
            return _ =>
            {
                f();
                return EmptyResponse;
            };
        }

        public static AsyncRequestHandler FromAction(Action<IHttpRequest> a)
        {
            return req =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static AsyncRequestHandler FromAsyncAction(Func<IHttpRequest, Task> a)
        {
            return req =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static AsyncRequestHandler From(Func<HttpResponse> f)
        {
            return _ =>
            {
                HttpResponse resp = f();
                return Task.FromResult(resp);
            };
        }

        public static AsyncRequestHandler From(Func<IHttpRequest, HttpResponse> f)
        {
            return req =>
            {
                HttpResponse resp = f(req);
                return Task.FromResult(resp);
            };
        }

        public static AsyncRequestHandler FromAsync(Func<Task<HttpResponse>> f)
        {
            return _ => f();
        }

        public static AsyncRequestHandler From(Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f)
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
