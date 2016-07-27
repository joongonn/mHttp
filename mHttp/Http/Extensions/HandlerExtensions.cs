using System;

namespace m.Http.Extensions
{
    using RequestHandler = Func<IHttpRequest, HttpResponse>;

    using ResponseFilter = Func<IHttpRequest, HttpResponse, HttpResponse>;

    public static class HandlerExtensions
    {
        [Obsolete] public static RequestHandler FilterResponse(this RequestHandler f, ResponseFilter filter)
        {
            return (IHttpRequest req) => {
                var resp = f(req);
                return filter(req, resp);
            };
        }
    }
}
