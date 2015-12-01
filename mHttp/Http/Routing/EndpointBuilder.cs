namespace m.Http.Routing
{
    public sealed class EndpointBuilder
    {
        public readonly Method Method;
        public readonly Route Route;

        internal EndpointBuilder(Method method, Route route)
        {
            Method = method;
            Route = route;
        }
    }
}
