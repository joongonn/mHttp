using System;
using System.Collections.Generic;

namespace m.Http
{
    public interface IHttpRequest
    {
        IReadOnlyDictionary<string, string> Headers { get; } //TODO: IDictionary<string, IEnumerable<string>> ?

        Uri Url { get; }
        string Path { get; }
        string Query { get; }
    }
}
