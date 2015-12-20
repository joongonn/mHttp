using System;
using System.Threading.Tasks;

using m.Http.Routing;

namespace m.Http
{
    using MethodRoute = Tuple<Method, Routing.Route>;

    public static class JsonEndpointBuilder
    {
        public static Endpoint WithAsync<TReq>(this MethodRoute pair, Func<JsonRequest<TReq>, Task<HttpResponse>> f)
        {
            return new Endpoint(pair.Item1, pair.Item2, JsonHandler<TReq>.FromAsync(f));
        }
    }
}
