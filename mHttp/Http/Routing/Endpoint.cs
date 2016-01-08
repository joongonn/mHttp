using System;
using System.Threading.Tasks;

namespace m.Http.Routing
{
    using AsyncHandler = Func<IHttpRequest, Task<HttpResponse>>;
    
    public class Endpoint : IComparable<Endpoint>
    {
        public readonly Method Method;
        public readonly Route Route;
        public readonly AsyncHandler Handler;

        public Endpoint(Method method, Route route, AsyncHandler handler)
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
