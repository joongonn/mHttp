using System;
using System.Net;

namespace m.Http.Backend.Tcp
{
    class ParseRequestException : RequestException
    {
        public ParseRequestException(string message) : base(message, HttpStatusCode.BadRequest) { }

        public ParseRequestException(string message, HttpStatusCode statusCode) : base(message, statusCode) { }

        public ParseRequestException(string message, Exception innerException) : base(message, innerException, HttpStatusCode.BadRequest) { }
    }
}
