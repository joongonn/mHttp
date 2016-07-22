using System;
using System.Threading.Tasks;

using m.Http.Routing;
using m.Utils;

namespace m.Http
{
    public static class Route
    {
        public sealed class MethodRoute
        {
            public readonly Method Method;
            public readonly Routing.Route Route;

            internal MethodRoute(Method method, Routing.Route route)
            {
                Method = method;
                Route = route;
            }
        }

        public sealed class WebSocketRoute
        {
            public readonly Routing.Route Route;

            internal WebSocketRoute(Routing.Route route)
            {
                Route = route;
            }
        }

        public static MethodRoute Get(string route)
        {
            return new MethodRoute(Method.GET, new Routing.Route(route));
        }

        public static WebSocketRoute GetWebSocketUpgrade(string route)
        {
            return new WebSocketRoute(new Routing.Route(route));
        }

        public static MethodRoute Post(string route)
        {
            return new MethodRoute(Method.POST, new Routing.Route(route));
        }

        public static MethodRoute Put(string route)
        {
            return new MethodRoute(Method.PUT, new Routing.Route(route));
        }

        public static MethodRoute Delete(string route)
        {
            return new MethodRoute(Method.DELETE, new Routing.Route(route));
        }

        public static RateLimitedEndpoint LimitRate(this Endpoint ep, int requestsPerSecond, int burstRequestsPerSecond=0)
        {
            if (burstRequestsPerSecond == 0)
            {
                burstRequestsPerSecond = requestsPerSecond;
            }

            return new RateLimitedEndpoint(ep.Method, ep.Route, ep.Handler, requestsPerSecond, burstRequestsPerSecond);
        }

        public static Endpoint ServeDirectory(string route, string directory)
        {
            return ServeDirectory(route, directory, Compression.GZip);
        }

        public static Endpoint ServeDirectory(string route, string directory, Func<byte[], byte[]> gzipFunc)
        {
            return Get(route).With(Handler.ServeDirectory(route, directory, gzipFunc));
        }

        public static Endpoint WithAction(this MethodRoute pair, Action a)
        {
            return new Endpoint(pair.Method, pair.Route, Handler.FromAction(a));
        }

        public static Endpoint WithAsyncAction(this MethodRoute pair, Func<Task> a)
        {
            return new Endpoint(pair.Method, pair.Route, Handler.FromAsyncAction(a));
        }

        public static Endpoint With(this MethodRoute pair, Func<HttpResponse> f)
        {
            return new Endpoint(pair.Method, pair.Route, Handler.From(f));
        }

        public static Endpoint WithAsync(this MethodRoute pair, Func<Task<HttpResponse>> f)
        {
            return new Endpoint(pair.Method, pair.Route, Handler.FromAsync(f));
        }

        public static Endpoint WithAction(this MethodRoute pair, Action<IHttpRequest> a)
        {
            return new Endpoint(pair.Method, pair.Route, Handler.FromAction(a));
        }

        public static Endpoint WithAsyncAction(this MethodRoute pair, Func<IHttpRequest, Task> a)
        {
            return new Endpoint(pair.Method, pair.Route, Handler.FromAsyncAction(a));
        }

        public static Endpoint With(this MethodRoute pair, Func<IHttpRequest, HttpResponse> f)
        {
            return new Endpoint(pair.Method, pair.Route, Handler.From(f));
        }

        public static Endpoint WithAsync(this MethodRoute pair, Func<IHttpRequest, Task<HttpResponse>> f)
        {
            return new Endpoint(pair.Method, pair.Route, f);
        }

        public static Endpoint With(this WebSocketRoute wsRoute, Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f)
        {
            return new Endpoint(Method.GET, wsRoute.Route, Handler.From(f));
        }
    }
}
