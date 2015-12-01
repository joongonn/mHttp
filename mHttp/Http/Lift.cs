using System;
using System.Threading.Tasks;

namespace m.Http
{
    public static class Lift
    {
        #region Json
        public static Func<Request, IHttpResponse> ToJsonHandler(Func<object> f)
        {
            return (Request req) =>
            {
                object resp = f();
                IHttpResponse httpResp = HttpResponse.Json(resp);
                return httpResp;
            };
        }

        public static Func<Request, IHttpResponse> ToJsonHandler<TReq, TResp>(Func<TReq, TResp> f)
        {
            return (Request req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                TResp resp = f(jsonReq.Req);
                IHttpResponse httpResp = HttpResponse.Json(resp);
                return httpResp;
            };
        }

        public static Func<Request, Task<IHttpResponse>> ToAsyncJsonHandler<TReq, TResp>(Func<TReq, Task<TResp>> f)
        {
            return async (Request req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                TResp resp = await f(jsonReq.Req);
                return HttpResponse.Json(resp);
            };
        }
        #endregion
    }
}
