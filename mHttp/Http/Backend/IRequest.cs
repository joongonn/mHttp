using System;
using System.Collections.Generic;

namespace m.Http.Backend
{
    public interface IRequest //TODO: this is useless
    {
        Uri Url { get; }
        string Path { get; }
        string Query { get; }

        IReadOnlyDictionary<string, string> Headers { get; }
    }
}
