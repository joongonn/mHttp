using System;

namespace m.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
    public sealed class EndpointAttribute : Attribute
    {
        public readonly Method Method;
        public readonly string PathTemplate;
        public readonly int RequestsPerSecond;
        public readonly int BurstRequestsPerSecond;

        public EndpointAttribute(Method method, string pathTemplate, int requestsPerSecond=0, int burstRequestsPerSecond=0)
        {
            Method = method;
            PathTemplate = pathTemplate;
            RequestsPerSecond = requestsPerSecond;
            BurstRequestsPerSecond = burstRequestsPerSecond == 0 ? requestsPerSecond : burstRequestsPerSecond;
        }
    }
}
