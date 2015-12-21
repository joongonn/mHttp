using System;

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
}
