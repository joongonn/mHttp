using System;
using System.Net;

namespace m.Http.Backend
{
    class RequestException : Exception
    {
        public readonly HttpStatusCode HttpStatusCode;
        
        public RequestException(string msg, HttpStatusCode statusCode) :base(msg)
        {
            HttpStatusCode = statusCode;
        }

        public RequestException(string msg, Exception innerException, HttpStatusCode statusCode) : base(msg, innerException)
        {
            HttpStatusCode = statusCode;
        }
    }
}
