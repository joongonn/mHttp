using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using m.Http.Routing;

namespace m.Http
{
    using HandlerSignature1 = Func<IHttpRequest, Task<HttpResponse>>;
    using HandlerSignature2 = Func<IHttpRequest, HttpResponse>;
    using HandlerSignature3 = Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse>;

    public static class Endpoints
    {
        public static Endpoint[] From(Type classType)
        {
            var staticEndpointMethods = GetEndpointMethods(classType, true);

            return From(staticEndpointMethods, null);
        }

        public static Endpoint[] From(object classInstance)
        {
            var endpointMethods = GetEndpointMethods(classInstance.GetType(), false);

            return From(endpointMethods, classInstance);
        }

        static Endpoint[] From(IEnumerable<Tuple<MethodInfo, EndpointAttribute>> endpointMethods, object targetClassInstance)
        {
            var endpoints = new List<Endpoint>();

            foreach (var pair in endpointMethods)
            {
                var method = pair.Item1;
                var endpoint = pair.Item2;

                Func<IHttpRequest, Task<HttpResponse>> handler;
                if (IsValidEndpointHandler(method, targetClassInstance, out handler))
                {
                    endpoints.Add(new Endpoint(endpoint.Method, new Routing.Route(endpoint.PathTemplate), handler));
                }
                else
                {
                    throw new Exception(string.Format("[{0}.{1}] does not have a recognized endpoint method signature", method.DeclaringType.Name, method.Name));
                }
            }

            return endpoints.ToArray();
        }

        static bool IsValidEndpointHandler(MethodInfo method, object targetClassInstance, out Func<IHttpRequest, Task<HttpResponse>> asHandler)
        {
            try // `Func<Request, Task<HttpResponse>>` ?
            {
                if (method.IsStatic)
                {
                    asHandler = (HandlerSignature1)Delegate.CreateDelegate(typeof(HandlerSignature1), method);
                }
                else
                {
                    asHandler = (HandlerSignature1)Delegate.CreateDelegate(typeof(HandlerSignature1), targetClassInstance, method);
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
                    var syncHandler = (HandlerSignature2)Delegate.CreateDelegate(typeof(HandlerSignature2), method);
                    asHandler = Handlers.From(syncHandler);
                }
                else
                {
                    var syncHandler = (HandlerSignature2)Delegate.CreateDelegate(typeof(HandlerSignature2), targetClassInstance, method);
                    asHandler = Handlers.From(syncHandler);
                }
                return true;
            }
            catch
            {
                asHandler = null;
            }

            try // `Func<IWebSocketUpgradeRequest, WebSocketUpgradeResponse>` ?
            {
                if (method.IsStatic)
                {
                    var syncHandler = (HandlerSignature3)Delegate.CreateDelegate(typeof(HandlerSignature3), method);
                    asHandler = Handlers.From(syncHandler);
                }
                else
                {
                    var syncHandler = (HandlerSignature3)Delegate.CreateDelegate(typeof(HandlerSignature3), targetClassInstance, method);
                    asHandler = Handlers.From(syncHandler);
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
