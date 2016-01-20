namespace m.Http
{
    public static class HttpHeader
    {
        public const string Host = "Host";
        public const string Connection = "Connection";
        public const string ContentType = "Content-Type";
        public const string ContentEncoding = "Content-Encoding";
        public const string ContentLength = "Content-Length";

        public const string Upgrade = "Upgrade";

        public const string WebSocketVersion = "Sec-WebSocket-Version";
        public const string WebSocketKey = "Sec-WebSocket-Key";
        public const string WebSocketExtensions = "Sec-WebSocket-Extensions";

        public const string LastModified = "Last-Modified";
        public const string IfModifiedSince = "If-Modified-Since";

        public const string AcceptEncoding = "Accept-Encoding";

        public const string UserAgent = "User-Agent";
        public const string Referer = "Referer";
    }

    public static class HttpHeaderValue
    {
        public const string GZip = "gzip";
    }
}
