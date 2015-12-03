using System;
using System.Threading.Tasks;

namespace m.Http
{
    public static class Lift
    {
        #region Json
        public static Func<Request, HttpResponse> ToJsonHandler(Func<object> f)
        {
            return (Request req) =>
            {
                object resp = f();
                HttpResponse httpResp = new JsonResponse(resp);
                return httpResp;
            };
        }

        public static Func<Request, HttpResponse> ToJsonHandler<TReq, TResp>(Func<TReq, TResp> f)
        {
            return (Request req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                TResp resp = f(jsonReq.Req);
                HttpResponse httpResp = new JsonResponse(resp);
                return httpResp;
            };
        }

        public static Func<Request, Task<HttpResponse>> ToAsyncJsonHandler<TReq, TResp>(Func<TReq, Task<TResp>> f)
        {
            return async (Request req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                TResp resp = await f(jsonReq.Req);
                return new JsonResponse(resp);
            };
        }
        #endregion
    }
}
