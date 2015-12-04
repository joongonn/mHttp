using System;
using System.Net;
using System.Threading.Tasks;

using m.Utils;

namespace m.Http.Routing
{
    public sealed class RateLimitedEndpoint : Endpoint
    {
        static readonly HttpResponse TooManyRequests = new ErrorResponse((HttpStatusCode)429, "Too many requests");
        
        readonly LeakyBucket rateLimitBucket;
        readonly Func<Request, Task<HttpResponse>> originalHandler;

        internal RateLimitedEndpoint(Method method, Route route, Func<Request, Task<HttpResponse>> handler, int requestsPerSecond, int burstRequestsPerSecond)
        {
            Method = method;
            Route = route;
            originalHandler = handler;
            rateLimitBucket = new LeakyBucket(burstRequestsPerSecond, requestsPerSecond);
            Handler = Handle;
        }

        Task<HttpResponse> Handle(Request request)
        {
            if (rateLimitBucket.Fill(1))
            {
                return originalHandler(request);
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
    }
}
