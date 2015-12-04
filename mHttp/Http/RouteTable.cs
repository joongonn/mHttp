using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using m.Http.Routing;

namespace m.Http
{
    public sealed class RouteTable : IEnumerable<Endpoint>
    {
        sealed class IndexedEndpoint
        {
            public readonly int Index;
            public readonly Endpoint Endpoint;

            public IndexedEndpoint(int index, Endpoint endpoint)
            {
                Index = index;
                Endpoint = endpoint;
            }
        }

        readonly Endpoint[] allEndpoints;

        readonly IndexedEndpoint[] getEndpoints;
        readonly IndexedEndpoint[] postEndpoints;
        readonly IndexedEndpoint[] putEndpoints;
        readonly IndexedEndpoint[] deleteEndpoints;

        public int Length { get { return allEndpoints.Length; } }
        public Endpoint this[int EndpointIndex] { get { return allEndpoints[EndpointIndex]; } }

        public RouteTable(params Endpoint[] endpoints)
        {
            allEndpoints = new Endpoint[endpoints.Length];
            Array.Copy(endpoints, allEndpoints, endpoints.Length);

            var indexedEndpoints = allEndpoints.Select((endpoint, index) => new IndexedEndpoint(index, endpoint)).ToArray();

            getEndpoints = indexedEndpoints.Where(e => e.Endpoint.Method == Method.GET).ToArray();
            postEndpoints = indexedEndpoints.Where(e => e.Endpoint.Method == Method.POST).ToArray();
            putEndpoints =indexedEndpoints.Where(e => e.Endpoint.Method == Method.PUT).ToArray();
            deleteEndpoints = indexedEndpoints.Where(e => e.Endpoint.Method == Method.DELETE).ToArray();
        }

        public IEnumerator<Endpoint> GetEnumerator()
        {
            return ((IEnumerable<Endpoint>)allEndpoints).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int TryMatchEndpoint(Method method,
                                    Uri url,
                                    out IReadOnlyDictionary<string, string> urlVariables)
        {
            IndexedEndpoint[] eps;
            switch (method)
            {
                case Method.GET    : eps = getEndpoints; break;
                case Method.POST   : eps = postEndpoints; break;
                case Method.PUT    : eps = putEndpoints; break;
                case Method.DELETE : eps = deleteEndpoints; break;

                default:
                    throw new NotSupportedException(string.Format("Unrecognized http method:[{0}]", method));
            }

            for (var i=0; i<eps.Length; i++)
            {
                var e = eps[i];
                if (e.Endpoint.Route.TryMatch(url, out urlVariables))
                {
                    return e.Index;
                }
            }

            urlVariables = null;
            return -1;
        }
    }
}
