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

        public readonly string HostPattern;
        readonly string[] hostPatternParts;

        readonly Endpoint[] allEndpoints;

        readonly IndexedEndpoint[] getEndpoints;
        readonly IndexedEndpoint[] postEndpoints;
        readonly IndexedEndpoint[] putEndpoints;
        readonly IndexedEndpoint[] deleteEndpoints;

        public int Length { get { return allEndpoints.Length; } }
        public Endpoint this[int EndpointIndex] { get { return allEndpoints[EndpointIndex]; } }

        public RouteTable(params Endpoint[] endpoints) : this("*", endpoints) { }

        public RouteTable(params Endpoint[][] endpointsGroups) : this("*", endpointsGroups.SelectMany(endpoints => endpoints).ToArray()) { }

        RouteTable(string hostPattern, params Endpoint[] endpoints)
        {
            HostPattern = hostPattern; //TODO: validate when exposed
            hostPatternParts = HostPattern.Split('.');

            allEndpoints = new Endpoint[endpoints.Length];
            Array.Copy(endpoints, allEndpoints, endpoints.Length);
            Array.Sort(allEndpoints);

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

        public bool MatchRequestedHost(string requestedHost)
        {
            if (hostPatternParts.Length == 1 && hostPatternParts[0][0] == '*')
            {
                return true;
            }

            requestedHost = requestedHost.Split(':')[0]; // drop the port
            var requestedHostParts = requestedHost.Split('.');

            if (hostPatternParts.Length != requestedHostParts.Length)
            {
                return false;
            }

            for (int i=hostPatternParts.Length-1; i>=0; i--)
            {
                if (hostPatternParts[i][0] == '*')
                {
                    return true;
                }

                if (!string.Equals(hostPatternParts[i], requestedHostParts[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public int TryMatchEndpoint(Method method, Uri url, out IReadOnlyDictionary<string, string> pathVariables)
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
                if (e.Endpoint.Route.TryMatch(url, out pathVariables))
                {
                    return e.Index;
                }
            }

            pathVariables = null;
            return -1;
        }
    }
}
