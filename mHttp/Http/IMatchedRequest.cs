using System.Collections.Generic;

namespace m.Http
{
    public interface IMatchedRequest : IHttpRequest
    {
        IReadOnlyDictionary<string, string> PathVariables { get; }
    }
}
