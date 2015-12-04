using System;
using System.Threading.Tasks;

namespace m.Http.Routing
{
    public class Endpoint
    {
        public Method Method { get; protected set; }
        public Route Route { get; protected set; }
        public Func<Request, Task<HttpResponse>> Handler { get; protected set; }

        protected Endpoint() { }

        public Endpoint(Method method, Route route, Func<Request, Task<HttpResponse>> handler)
        {
            Method = method;
            Route = route;
            Handler = handler;
        }
    }
}
