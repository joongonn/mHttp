using System;

using m.Http.Extensions;
using m.Utils;

namespace m.Http
{
    using SyncResponseFilter = Func<IHttpRequest, HttpResponse, HttpResponse>;

    public static class Filters
    {
        static readonly SyncResponseFilter defaultGZipFilter = GZip(Compression.GZip);

        public static HttpResponse GZip(IHttpRequest req, HttpResponse resp) => defaultGZipFilter(req, resp);

        public static SyncResponseFilter GZip(Func<byte[], byte[]> gzipFunc)
        {
            //FIXME: only gzip text, json, caller be aware (of penalty/cache implications) etc.
            //TODO: check if already gzipped when explicit filter phase added
            return (req, resp) => req.IsAcceptGZip() && resp?.Body.Length > 0 ? resp.GZip(gzipFunc) : resp;
        }
    }
}
