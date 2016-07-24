using System;
using System.Threading.Tasks;

using m.Http.Routing;

namespace m.Http
{
    public static class JsonEndpointBuilder
    {
        public static Endpoint WithAsync<TReq>(this EndpointBuilder pair, Func<JsonRequest<TReq>, Task<HttpResponse>> f)
        {
            return new Endpoint(pair.Method, pair.Route, JsonHandler<TReq>.FromAsync(f));
        }
    }
}
