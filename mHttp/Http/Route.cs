using System;
using System.Threading.Tasks;

using m.Http.Routing;

namespace m.Http
{
    public static class Route
    {
        public class MethodRoute
        {
            public readonly Method Method;
            public readonly Routing.Route Route;

            public MethodRoute(Method method, Routing.Route route)
            {
                Method = method;
                Route = route;
            }
        }

        public class WebSocketRoute
        {
            public readonly Routing.Route Route;

            public WebSocketRoute(Routing.Route route)
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

        public static Endpoint WithAction(this MethodRoute pair, Action a)
        {
            return new Endpoint(pair.Method, pair.Route, Handlers.FromAction(a));
        }

        public static Endpoint WithAsyncAction(this MethodRoute pair, Func<Task> a)
        {
            return new Endpoint(pair.Method, pair.Route, Handlers.FromAsyncAction(a));
        }

        public static Endpoint With(this MethodRoute pair, Func<HttpResponse> f)
        {
            return new Endpoint(pair.Method, pair.Route, Handlers.From(f));
        }

        public static Endpoint WithAsync(this MethodRoute pair, Func<Task<HttpResponse>> f)
        {
            return new Endpoint(pair.Method, pair.Route, Handlers.FromAsync(f));
        }

        public static Endpoint WithAction(this MethodRoute pair, Action<IHttpRequest> a)
        {
            return new Endpoint(pair.Method, pair.Route, Handlers.FromAction(a));
        }

        public static Endpoint WithAsyncAction(this MethodRoute pair, Func<IHttpRequest, Task> a)
        {
            return new Endpoint(pair.Method, pair.Route, Handlers.FromAsyncAction(a));
        }

        public static Endpoint With(this MethodRoute pair, Func<IHttpRequest, HttpResponse> f)
        {
            return new Endpoint(pair.Method, pair.Route, Handlers.From(f));
        }

        public static Endpoint WithAsync(this MethodRoute pair, Func<IHttpRequest, Task<HttpResponse>> f)
        {
            return new Endpoint(pair.Method, pair.Route, f);
        }

        public static Endpoint With(this WebSocketRoute wsRoute, Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f)
        {
            return new Endpoint(Method.GET, wsRoute.Route, Handlers.From(f));
        }
    }
}
