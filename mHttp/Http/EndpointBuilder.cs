using System;
using System.Threading.Tasks;

using m.Http.Routing;

namespace m.Http
{
    using MethodRoute = Tuple<Method, Routing.Route>;

    public static class EndpointBuilder
    {
        public static Endpoint WithAction(this MethodRoute pair, Action a)
        {
            return new Endpoint(pair.Item1, pair.Item2, Handlers.FromAction(a));
        }

        public static Endpoint WithAsyncAction(this MethodRoute pair, Func<Task> a)
        {
            return new Endpoint(pair.Item1, pair.Item2, Handlers.FromAsyncAction(a));
        }

        public static Endpoint With(this MethodRoute pair, Func<HttpResponse> f)
        {
            return new Endpoint(pair.Item1, pair.Item2, Handlers.From(f));
        }

        public static Endpoint WithAsync(this MethodRoute pair, Func<Task<HttpResponse>> f)
        {
            return new Endpoint(pair.Item1, pair.Item2, Handlers.FromAsync(f));
        }

        public static Endpoint WithAction(this MethodRoute pair, Action<IHttpRequest> a)
        {
            return new Endpoint(pair.Item1, pair.Item2, Handlers.FromAction(a));
        }

        public static Endpoint WithAsyncAction(this MethodRoute pair, Func<IHttpRequest, Task> a)
        {
            return new Endpoint(pair.Item1, pair.Item2, Handlers.FromAsyncAction(a));
        }

        public static Endpoint With(this MethodRoute pair, Func<IHttpRequest, HttpResponse> f)
        {
            return new Endpoint(pair.Item1, pair.Item2, Handlers.From(f));
        }

        public static Endpoint WithAsync(this MethodRoute pair, Func<IHttpRequest, Task<HttpResponse>> f)
        {
            return new Endpoint(pair.Item1, pair.Item2, f);
        }

        #region Websocket
        public static Endpoint With(this MethodRoute pair, Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f)
        {
            return new Endpoint(pair.Item1, pair.Item2, Handlers.From(f));
        }
        #endregion
    }
}
