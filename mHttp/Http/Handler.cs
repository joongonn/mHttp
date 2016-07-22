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
            return (IHttpRequest _) =>
            {
                a();
                return EmptyResponse;
            };
        }

        public static AsyncHandler FromAsyncAction(Func<Task> f)
        {
            return (IHttpRequest _) =>
            {
                f();
                return EmptyResponse;
            };
        }

        public static AsyncHandler FromAction(Action<IHttpRequest> a)
        {
            return (IHttpRequest req) =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static AsyncHandler FromAsyncAction(Func<IHttpRequest, Task> a)
        {
            return (IHttpRequest req) =>
            {
                a(req);
                return EmptyResponse;
            };
        }

        public static AsyncHandler From(Func<HttpResponse> f)
        {
            return (IHttpRequest _) =>
            {
                HttpResponse resp = f();
                return Task.FromResult(resp);
            };
        }

        public static AsyncHandler From(Func<IHttpRequest, HttpResponse> f)
        {
            return (IHttpRequest req) =>
            {
                HttpResponse resp = f(req);
                return Task.FromResult(resp);
            };
        }

        public static AsyncHandler FromAsync(Func<Task<HttpResponse>> f)
        {
            return (IHttpRequest _) => f();
        }

        public static AsyncHandler From(Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f)
        {
            return (IHttpRequest req) =>
            {
                var httpRequest = (HttpRequest)req;

                HttpResponse resp = f((IWebSocketUpgradeRequest)httpRequest);
                return Task.FromResult(resp);
            };
        }

        public static SyncHandler ServeDirectory(string route, string path, Func<byte[], byte[]> gzipFunc)
        {
            return new StaticFileHandler(route, path, gzipFunc).Handle;
        }
    }
}
