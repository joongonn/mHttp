using System;
using System.Threading.Tasks;

namespace m.Http.Handlers
{
    public static class JsonHandler<TReq>
    {
        public static Func<Request, Task<IHttpResponse>> FromAsync(Func<JsonRequest<TReq>, Task<IHttpResponse>> f)
        {
            return async (Request req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                IHttpResponse resp = await f(jsonReq);
                return resp;
            };
        }
    }
}
