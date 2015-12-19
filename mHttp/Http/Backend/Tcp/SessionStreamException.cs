using System;

namespace m.Http.Backend.Tcp
{
    class SessionStreamException : Exception
    {
        public SessionStreamException(string message) : base(message) { }
        
        public SessionStreamException(string message, Exception innerException) : base(message, innerException) { }
    }
}
