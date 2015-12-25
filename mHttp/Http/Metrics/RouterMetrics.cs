using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using m.Http.Metrics.Endpoints;

namespace m.Http.Metrics
{
    sealed class RouterMetrics
    {
        internal readonly StatusCodeCounter[][] statusCodesCounters;
        internal readonly HandlerTime[][] handlerTimes;

        internal RouterMetrics(Router router)
        {
            var routeTables = router.Length;
            statusCodesCounters = new StatusCodeCounter[routeTables][];
            handlerTimes = new HandlerTime[routeTables][];

            for (int i=0; i<routeTables; i++)
            {
                var endpoints = router[i].Length;
                statusCodesCounters[i] = new StatusCodeCounter[endpoints];
                handlerTimes[i] = new HandlerTime[endpoints];

                for (int j=0; j<endpoints; j++)
                {
                    statusCodesCounters[i][j] = new StatusCodeCounter();
                    handlerTimes[i][j] = new HandlerTime(1024);
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
                    handlerTimes[i][j].Update(endpointLogs);
                }
            }
        }
    }
}
