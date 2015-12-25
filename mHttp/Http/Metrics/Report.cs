using System.Linq;
using System.Threading;

namespace m.Http.Metrics
{
    public class Report
    {
        public class Endpoint
        {
            public class Bytes
            {
                public long RequestBytesIn { get; set; }
                public long ResponseBytesOut { get; set; }
            }
            
            public class Counter
            {
                public int StatusCode { get; set; }
                public int Count { get; set; }
            }

            public class HandlerTime
            {
                public float Percentile { get; set; }
                public float Value { get; set; }
            }

            public string Method { get; set; }
            public string Route { get; set; }
            public Bytes Traffic { get; set; }
            public Counter[] Counters { get; set; }
            public HandlerTime[] HandlerTimes { get; set; }
        }

        public string Host { get; set; }
        public Endpoint[] Endpoints { get; set; }

        internal static Report[] Generate(Router router, RouterMetrics routerMetrics, BackendMetrics backendMetrics=null)
        {
            Thread.MemoryBarrier();

            return router.Select((routeTable, tableIndex) =>
                new Report
                {
                    Host = routeTable.HostPattern,
                    Endpoints = routeTable.Select((ep, epIndex) =>
                        new Report.Endpoint
                        {
                            Method = ep.Method.ToString(),
                            Route = ep.Route.PathTemplate,

                            Traffic =
                                backendMetrics == null ? null : new Report.Endpoint.Bytes
                                {
                                    RequestBytesIn = backendMetrics.totalRequestBytesIn[tableIndex][epIndex],
                                    ResponseBytesOut = backendMetrics.totalResponseBytesOut[tableIndex][epIndex],
                                },

                            Counters = routerMetrics.statusCodesCounters[tableIndex][epIndex].Where(entry => entry.Count > 0)
                                                                                             .Select(entry =>
                                new Report.Endpoint.Counter
                                {
                                    StatusCode = entry.Code,
                                    Count = entry.Count
                                }
                            ).ToArray(),

                            HandlerTimes = routerMetrics.handlerTimes[tableIndex][epIndex].GetTimes(0.5f, 0.9f, 0.999f)
                                                                                          .Zip(new [] { 50.0f, 90.0f, 99.9f }, (value, percentile) =>
                                new Report.Endpoint.HandlerTime
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
