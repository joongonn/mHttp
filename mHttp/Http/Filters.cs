using m.Http.Extensions;

namespace m.Http
{
    public static class Filters
    {
        public static HttpResponse GZip(IHttpRequest req, HttpResponse resp)
        {
            //FIXME: only gzip text, json, caller be aware (of penalty/cache implications) etc. 
            //TODO: check if already gzipped when explicit filter phase added
            return req.IsAcceptGZip() && resp.Body != null && resp.Body.Length > 0 ? resp.GZip() : resp;
        }
    }
}
