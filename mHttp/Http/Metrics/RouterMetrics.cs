using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using m.Http.Metrics.Endpoints;

namespace m.Http.Metrics
{
    public sealed class RouterMetrics
    {
        readonly Router router;

        readonly StatusCodeCounter[][] statusCodesCounters;
        readonly ResponseTime[][] responseTimes;

        internal RouterMetrics(Router router)
        {
            this.router = router;

            var routeTables = router.Length;
            statusCodesCounters = new StatusCodeCounter[routeTables][];
            responseTimes = new ResponseTime[routeTables][];

            for (int i=0; i<routeTables; i++)
            {
                var endpoints = router[i].Length;
                statusCodesCounters[i] = new StatusCodeCounter[endpoints];
                responseTimes[i] = new ResponseTime[endpoints];

                for (int j=0; j<endpoints; j++)
                {
                    statusCodesCounters[i][j] = new StatusCodeCounter();
                    responseTimes[i][j] = new ResponseTime(1024);
                }
            }
        }

        internal void Update(IEnumerable<RequestLogs.Log>[][] logs) // [RouteTableIndex][EndpointIndex]
        {
            for (int i=0; i<logs.Length; i++)
            {
                for (int j=0; j<logs[i].Length; j++)
                {
                    IEnumerable<RequestLogs.Log> endpointLogs = logs[i][j];

                    statusCodesCounters[i][j].Update(endpointLogs);
                    responseTimes[i][j].Update(endpointLogs);
                }
            }
        }

        public Report[] GetReports()
        {
            Thread.MemoryBarrier();

            return router.Select((routeTable, i) =>
                new Report
                {
                    Host = routeTable.HostPattern,
                    Endpoints = routeTable.Select((ep, j) =>
                        new Report.Endpoint
                        {
                            Method = ep.Method.ToString(),
                            Route = ep.Route.PathTemplate,

                            Counters = statusCodesCounters[i][j].Where(entry => entry.Count > 0)
                                                                .Select(entry =>
                                new Report.Endpoint.Counter
                                {
                                    StatusCode = entry.Code,
                                    Count = entry.Count
                                }
                            ).ToArray(),

                            ResponseTimes = responseTimes[i][j].GetTimes(0.5f, 0.9f, 0.999f)
                                                               .Zip(new [] { 50.0f, 90.0f, 99.9f }, (value, percentile) =>
                                new Report.Endpoint.ResponseTime
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
