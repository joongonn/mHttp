using System;
using System.Collections.Generic;
using System.IO;

namespace m.Http
{
    public interface IHttpRequest
    {
        Method Method { get; }
        Uri Url { get; }
        string Path { get; }
        IReadOnlyDictionary<string, string> PathVariables { get; }
        string Query { get; }
        IReadOnlyDictionary<string, string> Headers { get; }
        string ContentType { get; }

        Stream Body { get; } //TODO: just byte[] it
    }
}
