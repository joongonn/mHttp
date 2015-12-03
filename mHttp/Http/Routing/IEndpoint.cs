using System.Threading.Tasks;

using m.Http;

namespace m.Http.Routing
{
    public interface IEndpoint
    {
        Method Method { get; }
        Route Route { get; }

        Task<HttpResponse> Handle(Request request);
    }
}
