using System;
using System.Net;
using System.Threading.Tasks;

using m.Utils;

namespace m.Http.Routing
{
    public sealed class RateLimitedEndpoint : IEndpoint
    {
        public Method Method { get; private set; }
        public Route Route { get; private set; }
        readonly Func<Request, Task<IHttpResponse>> Handler;
        readonly LeakyBucket rateLimitBucket;

        readonly string toString;

        internal RateLimitedEndpoint(Method method, Route route, Func<Request, Task<IHttpResponse>> handler, int requestsPerSecond, int burstRequestsPerSecond)
        {
            Method = method;
            Route = route;
            Handler = handler;
            rateLimitBucket = new LeakyBucket(burstRequestsPerSecond, requestsPerSecond);
            toString = string.Format("{0}({1}:{2})", GetType().Name, Method, Route.PathTemplate);
        }

        public Task<IHttpResponse> Handle(Request request)
        {
            if (rateLimitBucket.Fill(1))
            {
                return Handler(request);
            }
            else
            {
                IHttpResponse errorResponse = HttpResponse.Error((HttpStatusCode)429, "Too many requests");
                return Task.FromResult(errorResponse);
            }
        }

        public void UpdateRateLimitBucket()
        {
            rateLimitBucket.Leak();
        }

        public override string ToString()
        {
            return toString;
        }
    }
}
