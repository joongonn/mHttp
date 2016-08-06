using System;
using System.Collections.Generic;

using m.Http.Extensions;
using m.Utils;

namespace m.Http
{
    using ResponseFilter = Func<IHttpRequest, HttpResponse, HttpResponse>;
    // using AsyncResponseFilter = Func<IHttpRequest, HttpResponse, Task<HttpResponse>>;

    public static class Filters
    {
        //TODO: will be needing a GZippedResponse (for large underlying streams)
        internal static HttpResponse GZipFunc(HttpResponse origResp)
        {
            var byteArrayBody = origResp.Body as HttpBody.ByteArray;
            if (byteArrayBody != null)
            {
                var newHeaders = new Dictionary<string, string>(origResp.Headers, StringComparer.OrdinalIgnoreCase)
                {
                    { HttpHeader.ContentEncoding, HttpHeaderValue.GZip }
                };

                return new HttpResponse(origResp.StatusCode,
                                        origResp.StatusDescription,
                                        origResp.ContentType,
                                        newHeaders,
                                        new HttpBody.ByteArray(Compression.GZip(byteArrayBody.Bytes)));
            }

            //TODO: if body is a HttpBody.Streamable

            return origResp; // pass-through (unzipped) for now
        }

        public static HttpResponse GZip(IHttpRequest req, HttpResponse resp) => GZip(GZipFunc)(req, resp);

        public static ResponseFilter GZip(Func<HttpResponse, HttpResponse> gzipFunc)
        {
            //TODO: more guards (eg. current contentType / already gzipped / etc)
            return (req, resp) => req.IsAcceptGZip() && resp.Body.Length > 0 ? gzipFunc(resp) : resp;
        }
    }
}
