using System;
using System.Threading.Tasks;

namespace m.Http
{
    public static class Lift
    {
        #region Json
        public static Func<IHttpRequest, HttpResponse> ToJsonHandler(Func<object> f)
        {
            return (IHttpRequest _) =>
            {
                object resp = f();
                HttpResponse httpResp = new JsonResponse(resp);
                return httpResp;
            };
        }

        public static Func<IHttpRequest, HttpResponse> ToJsonHandler<TReq, TResp>(Func<TReq, TResp> f)
        {
            return (IHttpRequest req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                TResp resp = f(jsonReq.Req);
                HttpResponse httpResp = new JsonResponse(resp);
                return httpResp;
            };
        }

        public static Func<IHttpRequest, Task<HttpResponse>> ToAsyncJsonHandler<TReq, TResp>(Func<TReq, Task<TResp>> f)
        {
            return async (IHttpRequest req) =>
            {
                JsonRequest<TReq> jsonReq = JsonRequest<TReq>.From(req);
                TResp resp = await f(jsonReq.Req).ConfigureAwait(false);
                return new JsonResponse(resp);
            };
        }
        #endregion
    }
}
