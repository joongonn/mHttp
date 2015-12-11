using System;
using System.Threading.Tasks;

namespace m.Http.Handlers
{
    public static class JsonHandler<TReq>
    {
        public static Func<IHttpRequest, Task<HttpResponse>> FromAsync(Func<JsonRequest<TReq>, Task<HttpResponse>> f)
        {
            return async (IHttpRequest req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                HttpResponse resp = await f(jsonReq);
                return resp;
            };
        }
    }
}
