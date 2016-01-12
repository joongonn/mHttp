using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using m.Utils;

namespace m.Http.Metrics
{
    sealed class RequestLogs
    {
        public abstract class Log
        {
            public readonly IHttpRequest Request;
            public readonly HttpResponse Response;
            public readonly DateTime ArrivedOn;
            public readonly DateTime CompletedOn;

            public readonly long CompletedOnTimeMillis;
            public readonly int CompletedOnTimeHours;

            protected Log(IHttpRequest request, HttpResponse response, DateTime arrivedOn, DateTime completedOn)
            {
                Request = request;
                Response = response;
                ArrivedOn = arrivedOn;
                CompletedOn = completedOn;

                CompletedOnTimeMillis = CompletedOn.ToTimeMillis();
                CompletedOnTimeHours = CompletedOn.ToTimeHours();
            }
        }

        class IndexedLog : Log
        {
            public readonly int RouteTableIndex;
            public readonly int EndpointIndex;

            public IndexedLog(int routeTableIndex, int endpointIndex, IHttpRequest request, HttpResponse response, DateTime arrivedOn, DateTime completedOn) : base(request, response, arrivedOn, completedOn)
            {
                RouteTableIndex = routeTableIndex;
                EndpointIndex = endpointIndex;
            }
        }

        readonly int capacity;
        readonly BlockingCollection<IndexedLog> queue; // Single-reader multiple-writer

        readonly object drainLock = new object();
        readonly Queue<Log>[][] drainBuffers; // by [RouteTableIndex][EndpointIndex]

        public int Count { get { return queue.Count; } }

        public RequestLogs(Router router, int capacity)
        {
            this.capacity = capacity;
            queue = new BlockingCollection<IndexedLog>(capacity);

            drainBuffers = new Queue<Log>[router.Length][];
            for (int i=0; i<drainBuffers.Length; i++)
            {
                drainBuffers[i] = new Queue<Log>[router[i].Length];
                for (int j=0; j<drainBuffers[i].Length; j++)
                {
                    drainBuffers[i][j] = new Queue<Log>(capacity);
                }
            }
        }

        public bool TryAdd(int routeTableIndex, int endpointIndex, IHttpRequest request, HttpResponse response, DateTime arrivedOn, DateTime completedOn)
        {
            return queue.TryAdd(new IndexedLog(routeTableIndex, endpointIndex, request, response, arrivedOn, completedOn));
        }

        public int Drain(out IEnumerable<Log>[][] logs)
        {
            lock (drainLock)
            {
                if (queue.Count > 0)
                {
                    for (int i=0; i<drainBuffers.Length; i++)
                    {
                        for (int j=0; j<drainBuffers[i].Length; j++)
                        {
                            drainBuffers[i][j].Clear();
                        }
                    }

                    var taken = 0;
                    IndexedLog log;
                    while (taken < capacity && queue.TryTake(out log))
                    {
                        drainBuffers[log.RouteTableIndex][log.EndpointIndex].Enqueue(log);
                        taken++;
                    }

                    logs = drainBuffers;
                    return taken;
                }
                else
                {
                    logs = null;
                    return 0;
                }
            }
        }
    }
}
