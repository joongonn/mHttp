using System;
using System.Threading.Tasks;

namespace m.Http.Handlers
{
    public static class JsonHandler<TReq>
    {
        public static Func<Request, Task<HttpResponse>> FromAsync(Func<JsonRequest<TReq>, Task<HttpResponse>> f)
        {
            return async (Request req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                HttpResponse resp = await f(jsonReq);
                return resp;
            };
        }
    }
}
