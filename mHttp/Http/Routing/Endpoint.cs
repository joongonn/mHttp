using System;
using System.Threading.Tasks;

namespace m.Http.Routing
{
    public sealed class Endpoint : IEndpoint
    {
        public Method Method { get; private set; }
        public Route Route { get; private set; }
        internal readonly Func<Request, Task<IHttpResponse>> Handler;

        readonly string toString;

        public Endpoint(Method method, Route route, Func<Request, Task<IHttpResponse>> handler)
        {
            Method = method;
            Route = route;
            Handler = handler;

            toString = string.Format("{0}({1}:{2})", GetType().Name, Method, Route.PathTemplate);
        }

        public Task<IHttpResponse> Handle(Request request)
        {
            return Handler(request);
        }

        public override string ToString()
        {
            return toString;
        }
    }
}
