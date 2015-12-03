using System;
using System.Net;
using System.Threading.Tasks;

using m.Utils;

namespace m.Http.Routing
{
    public sealed class RateLimitedEndpoint : IEndpoint
    {
        static readonly HttpResponse TooManyRequests = new ErrorResponse((HttpStatusCode)429, "Too many requests");
        
        public Method Method { get; private set; }
        public Route Route { get; private set; }
        readonly Func<Request, Task<HttpResponse>> Handler;
        readonly LeakyBucket rateLimitBucket;

        readonly string toString;

        internal RateLimitedEndpoint(Method method, Route route, Func<Request, Task<HttpResponse>> handler, int requestsPerSecond, int burstRequestsPerSecond)
        {
            Method = method;
            Route = route;
            Handler = handler;
            rateLimitBucket = new LeakyBucket(burstRequestsPerSecond, requestsPerSecond);
            toString = string.Format("{0}({1}:{2})", GetType().Name, Method, Route.PathTemplate);
        }

        public Task<HttpResponse> Handle(Request request)
        {
            if (rateLimitBucket.Fill(1))
            {
                return Handler(request);
            }
            else
            {
                return Task.FromResult(TooManyRequests);
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
