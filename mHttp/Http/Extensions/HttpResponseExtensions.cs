using System;
using System.Collections.Generic;

using m.Utils;

namespace m.Http.Extensions
{
    public static class HttpResponseExtensions
    {
        public static HttpResponse GZip(this HttpResponse response) //TODO: or give it its own type ie. public class GzipResponse : HttpResponse
        {
            var gzippedBody = response.Body.GZip();
            var newHeaders = new Dictionary<string, string>(response.Headers, StringComparer.OrdinalIgnoreCase);

            newHeaders[HttpHeader.ContentEncoding] = HttpHeaderValue.GZip;

            var gzippedResponse = new HttpResponse(response.StatusCode,
                                                   response.StatusDescription,
                                                   response.ContentType,
                                                   newHeaders,
                                                   gzippedBody);

            return gzippedResponse;
        }
    }
}
