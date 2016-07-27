using System;

namespace m.Http.Backend
{
    class WebSocketException : Exception
    {
        public WebSocketException(string message) : base(message) { }

        public WebSocketException(string message, Exception innerException) : base(message, innerException) { }
    }
}
