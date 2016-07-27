using System;

using m.Http.Extensions;
using m.Utils;

namespace m.Http
{
    using ResponseFilter = Func<IHttpRequest, HttpResponse, HttpResponse>;
    // using AsyncResponseFilter = Func<IHttpRequest, HttpResponse, Task<HttpResponse>>;

    public static class Filters
    {
        static readonly ResponseFilter defaultGZipFilter = GZip(Compression.GZip);

        public static HttpResponse GZip(IHttpRequest req, HttpResponse resp) => defaultGZipFilter(req, resp);

        public static ResponseFilter GZip(Func<byte[], byte[]> gzipFuncImpl)
        {
            //TODO: only gzip text, json, caller be aware (of penalty/cache implications) etc.
            //TODO: check if already gzipped when explicit filter phase added
            return (req, resp) => req.IsAcceptGZip() && resp?.Body.Length > 0 ? resp.GZip(gzipFuncImpl) : resp;
        }
    }
}
