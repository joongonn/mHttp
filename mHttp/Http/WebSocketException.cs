using System;

namespace m.Http
{
    public class WebSocketException : Exception
    {
        public WebSocketException(string message) : base(message) { }

        public WebSocketException(string message, Exception innerException) : base(message, innerException) { }
    }
}
