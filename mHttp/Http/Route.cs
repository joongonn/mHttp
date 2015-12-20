using System;

using m.Http.Routing;

namespace m.Http
{
    using MethodRoute = Tuple<Method, Routing.Route>;

    public static class Route
    {
        public static MethodRoute Get(string route)
        {
            return Tuple.Create(Method.GET, new Routing.Route(route));
        }

        public static MethodRoute Post(string route)
        {
            return Tuple.Create(Method.POST, new Routing.Route(route));
        }

        public static MethodRoute Put(string route)
        {
            return Tuple.Create(Method.PUT, new Routing.Route(route));
        }

        public static MethodRoute Delete(string route)
        {
            return Tuple.Create(Method.DELETE, new Routing.Route(route));
        }

        public static RateLimitedEndpoint LimitRate(this Endpoint ep, int requestsPerSecond, int burstRequestsPerSecond=0)
        {
            if (burstRequestsPerSecond == 0)
            {
                burstRequestsPerSecond = requestsPerSecond;
            }

            return new RateLimitedEndpoint(ep.Method, ep.Route, ep.Handler, requestsPerSecond, burstRequestsPerSecond);
        }
    }
}
