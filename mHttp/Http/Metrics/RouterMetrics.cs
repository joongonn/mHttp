using System;
using System.Collections.Generic;
using System.Threading;

using m.Http.Metrics.Endpoints;
using m.Utils;

namespace m.Http.Metrics
{
    sealed class RouterMetrics
    {
        internal readonly StatusCodeCounter[][] statusCodesCounters;
        internal readonly HourlyStatusCodeCounter[][] hourlyStatusCodesCounters;
        internal readonly HandlerTime[][] handlerTimes;
        internal readonly RateCounter[][] responseRateCounters;

        internal readonly long [][] totalRequestBytesIn;
        internal readonly long [][] totalResponseBytesOut;

        internal RouterMetrics(Router router)
        {
            var routeTables = router.Length;
            statusCodesCounters = new StatusCodeCounter[routeTables][];
            hourlyStatusCodesCounters = new HourlyStatusCodeCounter[routeTables][];
            handlerTimes = new HandlerTime[routeTables][];
            responseRateCounters = new RateCounter[routeTables][];
            totalRequestBytesIn = new long[routeTables][];
            totalResponseBytesOut = new long[routeTables][];

            for (int i=0; i<routeTables; i++)
            {
                var endpoints = router[i].Length;
                statusCodesCounters[i] = new StatusCodeCounter[endpoints];
                hourlyStatusCodesCounters[i] = new HourlyStatusCodeCounter[endpoints];
                handlerTimes[i] = new HandlerTime[endpoints];
                responseRateCounters[i] = new RateCounter[endpoints];

                totalRequestBytesIn[i] = new long[endpoints];
                totalResponseBytesOut[i] = new long[endpoints];

                for (int j=0; j<endpoints; j++)
                {
                    statusCodesCounters[i][j] = new StatusCodeCounter();
                    hourlyStatusCodesCounters[i][j] = new HourlyStatusCodeCounter(7 * 24);
                    handlerTimes[i][j] = new HandlerTime(1024);
                    responseRateCounters[i][j] = new RateCounter(100);

                    totalRequestBytesIn[i][j] = 0;
                    totalResponseBytesOut[i][j] = 0;
                }
            }
        }

        // Single threaded invocation by RouterTimer
        internal void Update(IEnumerable<RequestLogs.Log>[][] logs) // [RouteTableIndex][EndpointIndex]
        {
            for (int i=0; i<logs.Length; i++)
            {
                for (int j=0; j<logs[i].Length; j++)
                {
                    IEnumerable<RequestLogs.Log> endpointLogs = logs[i][j];

                    statusCodesCounters[i][j].Update(endpointLogs);
                    hourlyStatusCodesCounters[i][j].Update(endpointLogs);
                    handlerTimes[i][j].Update(endpointLogs);

                    foreach (var log in endpointLogs)
                    {
                        responseRateCounters[i][j].Count(log.CompletedOnTimeMillis, 1);
                    }
                }
            }
        }

        internal void CountRequestBytesIn(int routeTableIndex, int endpointIndex, int requestBytes)
        {
            Interlocked.Add(ref totalRequestBytesIn[routeTableIndex][endpointIndex], requestBytes);
        }

        internal void CountResponseBytesOut(int routeTableIndex, int endpointIndex, int responseBytes)
        {
            Interlocked.Add(ref totalResponseBytesOut[routeTableIndex][endpointIndex], responseBytes);
        }

        internal void CountBytes(int routeTableIndex, int endpointIndex, int requestBytes, int responseBytes)
        {
            Interlocked.Add(ref totalRequestBytesIn[routeTableIndex][endpointIndex], requestBytes);
            Interlocked.Add(ref totalResponseBytesOut[routeTableIndex][endpointIndex], responseBytes);
        }
    }
}
