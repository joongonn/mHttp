using System;
using System.Threading.Tasks;

namespace m.Http.Routing
{
    using AsyncHandler = Func<IHttpRequest, Task<HttpResponse>>;
    
    public class Endpoint : IComparable<Endpoint>
    {
        readonly string toString;

        internal Method Method { get; }
        internal Route Route { get; }
        internal AsyncHandler Handler { get; }

        internal Endpoint(Method method, Route route, AsyncHandler handler)
        {
            Method = method;
            Route = route;
            Handler = handler;

            toString = $"Endpoint({Method}:{Route.PathTemplate})";
        }

        public int CompareTo(Endpoint other) => Route.CompareTo(other.Route);

        public override string ToString() => toString;
    }
}
