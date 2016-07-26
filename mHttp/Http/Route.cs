using System;
using System.IO;
using System.Threading.Tasks;

using m.Http.Routing;
using m.Http.Handlers;
using m.Utils;

namespace m.Http
{
    using RequestHandler = Func<IHttpRequest, HttpResponse>;

    public static class Route
    {
        public static EndpointBuilder Get(string route) => new EndpointBuilder(Method.GET, route);

        public static WebSocketEndpointBuilder GetWebSocketUpgrade(string route) => new WebSocketEndpointBuilder(route);

        public static EndpointBuilder Post(string route) => new EndpointBuilder(Method.POST, route);

        public static EndpointBuilder Put(string route) => new EndpointBuilder(Method.PUT, route);

        public static EndpointBuilder Delete(string route) => new EndpointBuilder(Method.DELETE, route);

        [Obsolete] public static Endpoint ServeDirectory(string route, string directory) => ServeDirectory(route, directory, Compression.GZip);

        [Obsolete] public static Endpoint ServeDirectory(string route, string directory, Func<byte[], byte[]> gzipFunc) => Get(route).With(new StaticFileHandler(route, directory, gzipFunc).Handle);
    }

    public sealed class EndpointBuilder
    {
        internal Method Method { get; }
        internal Routing.Route Route { get; }

        internal EndpointBuilder(Method method, string route)
        {
            Method = method;
            Route = new Routing.Route(route);
        }

        public Endpoint WithAction(Action a) => new Endpoint(Method, Route, Handler.FromAction(a));

        public Endpoint WithAsyncAction(Func<Task> a) => new Endpoint(Method, Route, Handler.FromAsyncAction(a));

        public Endpoint With(Func<HttpResponse> f) => new Endpoint(Method, Route, Handler.From(f));

        public Endpoint WithAsync(Func<Task<HttpResponse>> f) => new Endpoint(Method, Route, Handler.FromAsync(f));

        public Endpoint WithAction(Action<IHttpRequest> a) => new Endpoint(Method, Route, Handler.FromAction(a));

        public Endpoint WithAsyncAction(Func<IHttpRequest, Task> a) => new Endpoint(Method, Route, Handler.FromAsyncAction(a));

        public Endpoint With(Func<IHttpRequest, HttpResponse> f) => new Endpoint(Method, Route, Handler.From(f));

        public Endpoint WithAsync(Func<IHttpRequest, Task<HttpResponse>> f) => new Endpoint(Method, Route, f);

        public Endpoint With(DirectoryInfo dirInfo) => With(dirInfo, Compression.GZip);

        public Endpoint With(DirectoryInfo dirInfo, Func<byte[], byte[]> gzipFuncImpl)
        {
            if (dirInfo.Exists)
            {
                //TODO: check format of `Route.PathTemplate` 
                var pathFilenameStartIndex = Route.PathTemplate.Length - 1; // (assumed) trailing '/*' to indicate start of filename
                RequestHandler fileHandler = new StaticFileHandler(pathFilenameStartIndex, dirInfo, gzipFuncImpl).Handle;
                return new Endpoint(Method, Route, Handler.From(fileHandler));
            }
            else
            {
                throw new DirectoryNotFoundException($"The specified directory {dirInfo.FullName} could not be found.");
            }
        }
    }

    public sealed class WebSocketEndpointBuilder
    {
        internal Routing.Route Route { get; }

        internal WebSocketEndpointBuilder(string route)
        {
            Route = new Routing.Route(route);
        }

        public Endpoint With(Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse> f) => new Endpoint(Method.GET, Route, Handler.From(f));
    }

    public static class EndpointHelper
    {
        public static RateLimitedEndpoint LimitRate(this Endpoint ep, int requestsPerSecond, int burstRequestsPerSecond=0)
        {
            if (burstRequestsPerSecond == 0)
            {
                burstRequestsPerSecond = requestsPerSecond;
            }

            return new RateLimitedEndpoint(ep.Method, ep.Route, ep.Handler, requestsPerSecond, burstRequestsPerSecond);
        }
    }
}
