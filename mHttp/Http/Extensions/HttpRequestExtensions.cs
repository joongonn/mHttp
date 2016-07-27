using System;
using System.Net;

using m.Logging;
using m.Http.Backend;

namespace m.Http.Extensions
{
    public static class HttpRequestExtensions
    {
        static readonly LoggingProvider.ILogger logger = LoggingProvider.GetLogger(typeof(HttpRequestExtensions));

        public static bool TryGetIfLastModifiedSince(this IHttpRequest req, out DateTime utcDate)
        {
            string value;
            if (req.Headers.TryGetValue(HttpHeader.IfModifiedSince, out value))
            {
                try
                {
                    utcDate = DateTime.Parse(value).ToUniversalTime();
                    return true;
                }
                catch (FormatException e)
                {
                    logger.Warn("Invalid If-Modified-Since header value:[{0}]", value);
                    throw new RequestException(string.Format("Invalid If-Modified-Since:[{0}]", value), e, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                utcDate = DateTime.UtcNow;
                return false;
            }
        }

        public static bool IsAcceptGZip(this IHttpRequest req)
        {
            string value;
            if (req.Headers.TryGetValue(HttpHeader.AcceptEncoding, out value))
            {
                return value.IndexOf(HttpHeaderValue.GZip, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            else
            {
                return false;
            }
        }
    }
}
