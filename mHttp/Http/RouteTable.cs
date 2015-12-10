using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using m.Http.Routing;

namespace m.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class EndpointAttribute : Attribute
    {
        public readonly Method Method;
        public readonly string PathTemplate;

        public EndpointAttribute(Method method, string pathTemplate)
        {
            Method = method;
            PathTemplate = pathTemplate;
        }
    }

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

        public RouteTable(string hostPattern, params Endpoint[] endpoints)
        {
            HostPattern = hostPattern; //TODO: validate
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

        public static List<Endpoint> GenerateEndpoints(Type classType)
        {
            var staticEndpointMethods = GetEndpointMethods(classType, true);

            return GenerateEndpoints(staticEndpointMethods, null);
        }

        public static List<Endpoint> GenerateEndpoints(object classInstance)
        {
            var endpointMethods = GetEndpointMethods(classInstance.GetType(), false);

            return GenerateEndpoints(endpointMethods, classInstance);
        }

        static List<Endpoint> GenerateEndpoints(IEnumerable<Tuple<MethodInfo, EndpointAttribute>> endpointMethods, object targetClassInstance)
        {
            var endpoints = new List<Endpoint>();

            foreach (var pair in endpointMethods)
            {
                var method = pair.Item1;
                var endpoint = pair.Item2;

                Func<Request, Task<HttpResponse>> handler;
                if (IsValidEndpointHandler(method, targetClassInstance, out handler))
                {
                    endpoints.Add(new Endpoint(endpoint.Method, new Routing.Route(endpoint.PathTemplate), handler));
                }
                else
                {
                    throw new Exception(string.Format("[{0}.{1}] does not have a valid endpoint method signature", method.DeclaringType.Name, method.Name));
                }
            }

            return endpoints;
        }

        static bool IsValidEndpointHandler(MethodInfo method, object targetClassInstance, out Func<Request, Task<HttpResponse>> asHandler)
        {
            try // `Func<Request, Task<HttpResponse>>` ?
            {
                if (method.IsStatic)
                {
                    asHandler = (Func<Request, Task<HttpResponse>>)Delegate.CreateDelegate(typeof(Func<Request, Task<HttpResponse>>), method);
                }
                else
                {
                    asHandler = (Func<Request, Task<HttpResponse>>)Delegate.CreateDelegate(typeof(Func<Request, Task<HttpResponse>>), targetClassInstance, method);
                }
                    
                return true;
            }
            catch
            {
                asHandler = null;
            }

            try // `Func<Request, HttpResponse>` ?
            {
                if (method.IsStatic)
                {
                    var syncHandler = (Func<Request, HttpResponse>)Delegate.CreateDelegate(typeof(Func<Request, HttpResponse>), method);
                    asHandler = Handlers.Handler.From(syncHandler);
                }
                else
                {
                    var syncHandler = (Func<Request, HttpResponse>)Delegate.CreateDelegate(typeof(Func<Request, HttpResponse>), targetClassInstance, method);
                    asHandler = Handlers.Handler.From(syncHandler);
                }
                return true;
            }
            catch
            {
                asHandler = null;
            }

            return false;
        }

        static IEnumerable<Tuple<MethodInfo, EndpointAttribute>> GetEndpointMethods(Type classType, bool staticOnly)
        {
            var methods = classType.GetMethods();

            var endpointMethods = methods.Select(m => new {
                                              Method = m,
                                              EndpointAttribute = m.GetCustomAttributes(typeof(EndpointAttribute), false)
                                                                   .SingleOrDefault() as EndpointAttribute
                                          })
                                         .Where(m => m.EndpointAttribute != null)
                                         .Select(m => Tuple.Create(m.Method, m.EndpointAttribute));

            if (staticOnly)
            {
                endpointMethods = endpointMethods.Where(m => m.Item1.IsStatic);
            }

            return endpointMethods;
        }
    }
}
