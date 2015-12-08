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

        public RateLimitedEndpoint(Method method,
                                   Route route,
                                   Func<Request, Task<HttpResponse>> handler,
                                   int requestsPerSecond,
                                   int burstRequestsPerSecond) : this(method, route, handler, new LeakyBucket(burstRequestsPerSecond, requestsPerSecond))
        {
        }

        RateLimitedEndpoint(Method method, Route route, Func<Request, Task<HttpResponse>> handler, LeakyBucket rateLimitBucket) : base(method, route, Wrap(handler, rateLimitBucket))
        {
            this.rateLimitBucket = rateLimitBucket;
        }

        static Func<Request, Task<HttpResponse>> Wrap(Func<Request, Task<HttpResponse>> handler, LeakyBucket rateLimitBucket)
        {
            return (Request request) =>
            {
                if (rateLimitBucket.Fill(1))
                {
                    return handler(request);
                }
                else
                {
                    return Task.FromResult(TooManyRequests);
                }
            };
        }

        public void UpdateRateLimitBucket()
        {
            rateLimitBucket.Leak();
        }
    }
}
