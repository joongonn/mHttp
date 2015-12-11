using System;
using System.Threading.Tasks;

namespace m.Http.Routing
{
    public class Endpoint : IComparable<Endpoint>
    {
        public readonly Method Method;
        public readonly Route Route;
        public readonly Func<IHttpRequest, Task<HttpResponse>> Handler;

        public Endpoint(Method method, Route route, Func<IHttpRequest, Task<HttpResponse>> handler)
        {
            Method = method;
            Route = route;
            Handler = handler;
        }

        public int CompareTo(Endpoint other)
        {
            return Route.CompareTo(other.Route);
        }
    }
}
