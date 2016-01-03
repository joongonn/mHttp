using System.Linq;

namespace m.Http.Metrics
{
    public class HostReport
    {
        public class Endpoint
        {
            public class StatusCodeCounter
            {
                public int StatusCode { get; set; }
                public int Count { get; set; }
            }

            public class HandlerTime
            {
                public float Percentile { get; set; }
                public float Value { get; set; }
            }

            public class BytesTransferred
            {
                public long In { get; set; }
                public long Out { get; set; }
            }

            public string Method { get; set; }
            public string Route { get; set; }
            public int CurrentResponseRate { get; set; }
            public BytesTransferred Bytes { get; set; }
            public StatusCodeCounter[] StatusCodeCounters { get; set; }
            public HandlerTime[] HandlerTimes { get; set; }
        }

        public string Host { get; set; }
        public Endpoint[] Endpoints { get; set; }

        internal static HostReport[] Generate(Router router, RouterMetrics routerMetrics, BackendMetrics backendMetrics=null)
        {
            return router.Select((routeTable, tableIndex) =>
                new HostReport
                {
                    Host = routeTable.HostPattern,
                    Endpoints = routeTable.Select((ep, epIndex) =>
                        new HostReport.Endpoint
                        {
                            Method = ep.Method.ToString(),
                            Route = ep.Route.PathTemplate,

                            CurrentResponseRate = routerMetrics.responseRateCounters[tableIndex][epIndex].GetCurrentRate(),

                            Bytes = backendMetrics == null ? null : new HostReport.Endpoint.BytesTransferred {
                                In = backendMetrics.totalRequestBytesIn[tableIndex][epIndex],
                                Out = backendMetrics.totalResponseBytesOut[tableIndex][epIndex]
                            },

                            StatusCodeCounters = routerMetrics.statusCodesCounters[tableIndex][epIndex].Where(entry => entry.Count > 0)
                                                                                                       .Select(entry =>
                                new HostReport.Endpoint.StatusCodeCounter
                                {
                                    StatusCode = entry.Code,
                                    Count = entry.Count
                                }
                            ).ToArray(),

                            HandlerTimes = routerMetrics.handlerTimes[tableIndex][epIndex].GetTimes(0.5f, 0.9f, 0.999f)
                                                                                          .Zip(new [] { 50.0f, 90.0f, 99.9f }, (value, percentile) =>
                                new HostReport.Endpoint.HandlerTime
                                {
                                    Percentile = percentile,
                                    Value = value
                                }
                            ).ToArray()
                        }
                    ).ToArray()
                }
            ).ToArray();
        }
    }
}
