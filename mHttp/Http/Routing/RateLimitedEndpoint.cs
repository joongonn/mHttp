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
                                   Func<IHttpRequest, Task<HttpResponse>> handler,
                                   int requestsPerSecond,
                                   int burstRequestsPerSecond) : this(method, route, handler, new LeakyBucket(burstRequestsPerSecond, requestsPerSecond))
        {
        }

        RateLimitedEndpoint(Method method, Route route, Func<IHttpRequest, Task<HttpResponse>> handler, LeakyBucket rateLimitBucket) : base(method, route, Wrap(handler, rateLimitBucket))
        {
            this.rateLimitBucket = rateLimitBucket;
        }

        static Func<IHttpRequest, Task<HttpResponse>> Wrap(Func<IHttpRequest, Task<HttpResponse>> handler, LeakyBucket rateLimitBucket)
        {
            return request => rateLimitBucket.Fill(1) ? handler(request) : Task.FromResult(TooManyRequests);
        }

        public void UpdateRateLimitBucket()
        {
            rateLimitBucket.Leak();
        }
    }
}
