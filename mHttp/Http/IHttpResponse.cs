using System.Net;
using System.Collections.Generic;

namespace m.Http
{
    public interface IHttpResponse
    {
        HttpStatusCode StatusCode { get; }
        string StatusDescription { get; }
        string ContentType { get; }
        IDictionary<string, string> Headers { get; }

        byte[] Body { get; }
    }
}
