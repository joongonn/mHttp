using System;
using System.Collections.Generic;

namespace m.Http.Extensions
{
    public static class HttpResponseExtensions
    {
        //TODO: or give it its own type ie. public class GzipResponse : HttpResponse
        public static HttpResponse GZip(this HttpResponse response, Func<byte[], byte[]> gzipFunc)
        {
            var newHeaders = new Dictionary<string, string>(response.Headers, StringComparer.OrdinalIgnoreCase)
            {
                { HttpHeader.ContentEncoding, HttpHeaderValue.GZip }
            };

            return new HttpResponse(response.StatusCode,
                                    response.StatusDescription,
                                    response.ContentType,
                                    newHeaders,
                                    gzipFunc(response.Body));
        }
    }
}
