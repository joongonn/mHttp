using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using m.Http.Metrics.Endpoints;

namespace m.Http.Metrics
{
    public sealed class ServerMetrics
    {
        readonly RouteTable routeTable;

        readonly StatusCodeCounter[] statusCodesCounters;
        readonly ResponseTime[] responseTimes;

        internal ServerMetrics(RouteTable routeTable)
        {
            this.routeTable = routeTable;

            var endpoints = this.routeTable.Length;
            statusCodesCounters = new StatusCodeCounter[endpoints];
            responseTimes = new ResponseTime[endpoints];

            for (int i=0; i<endpoints; i++)
            {
                statusCodesCounters[i] = new StatusCodeCounter();
                responseTimes[i] = new ResponseTime(1024);
            }
        }

        internal void Update(IEnumerable<RequestLogs.Log>[] logsByEndpointIndex)
        {
            for (int endpointIndex=0; endpointIndex<logsByEndpointIndex.Length; endpointIndex++)
            {
                IEnumerable<RequestLogs.Log> logs = logsByEndpointIndex[endpointIndex];

                statusCodesCounters[endpointIndex].Update(logs);
                responseTimes[endpointIndex].Update(logs);
            }
        }

        public Report GetReport()
        {
            Thread.MemoryBarrier();

            return new Report
            {
                Endpoints = routeTable.Select((ep, idx) =>
                    new Report.Endpoint {
                        Method = ep.Method.ToString(),
                        Route = ep.Route.PathTemplate,

                        Counters = statusCodesCounters[idx].Where(entry => entry.Count > 0)
                                                           .Select(entry =>
                            new Report.Endpoint.Counter {
                                StatusCode = entry.Code,
                                Count = entry.Count
                            }).ToArray(),

                        ResponseTimes = responseTimes[idx].GetTimes(0.5f, 0.9f, 0.999f)
                                                          .Zip(new [] { 50.0f, 90.0f, 99.9f }, (value, percentile) =>
                            new Report.Endpoint.ResponseTime {
                                Percentile = percentile,
                                Value = value
                            }).ToArray()
                    }).ToArray()
            };
        }
    }
}
