using System.Net;
using System.Text;

namespace m.Http
{
    public sealed class JsonResponse : HttpResponse
    {
        public JsonResponse(string json) : base(HttpStatusCode.OK, ContentTypes.Json, new HttpBody.ByteArray(Encoding.UTF8.GetBytes(json))) { }

        public JsonResponse(object t) : this(t.ToJson()) { }
    }
}
