using System;
using System.Threading.Tasks;

namespace m.Http.Extensions
{
    using SyncHandler = Func<IHttpRequest, HttpResponse>;
    using AsyncHandler = Func<IHttpRequest, Task<HttpResponse>>;

    using SyncResponseFilter = Func<IHttpRequest, HttpResponse, HttpResponse>;

    public static class HandlerExtensions
    {
        public static SyncHandler FilterResponse(this SyncHandler f, SyncResponseFilter filter)
        {
            return (IHttpRequest req) => {
                var resp = f(req);
                return filter(req, resp);
            };
        }
    }
}
