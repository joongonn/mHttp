using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

            protected Log(IHttpRequest request, HttpResponse response, DateTime arrivedOn, DateTime completedOn)
            {
                Request = request;
                Response = response;
                ArrivedOn = arrivedOn;
                CompletedOn = completedOn;
            }
        }

        class EndpointLog : Log
        {
            public readonly int EndpointIndex;

            public EndpointLog(int endpointIndex, IHttpRequest request, HttpResponse response, DateTime arrivedOn, DateTime completedOn) : base(request, response, arrivedOn, completedOn)
            {
                EndpointIndex = endpointIndex;
            }
        }

        readonly int endpointsCount;
        readonly int capacity;
        readonly BlockingCollection<EndpointLog> queue; // Single-reader multiple-writer

        readonly object drainLock = new object();
        readonly Queue<Log>[] drainBuffers; // by EndpointIndex

        public int Count { get { return queue.Count; } }

        public RequestLogs(int endpointsCount, int capacity)
        {
            this.endpointsCount = endpointsCount;
            this.capacity = capacity;
            queue = new BlockingCollection<EndpointLog>(capacity);
            drainBuffers = new Queue<Log>[endpointsCount];
            for (int i = 0; i < endpointsCount; i++)
            {
                drainBuffers[i] = new Queue<Log>(capacity);
            }
        }

        public bool TryAdd(int endpointIndex, IHttpRequest request, HttpResponse response, DateTime arrivedOn, DateTime completedOn)
        {
            return queue.TryAdd(new EndpointLog(endpointIndex, request, response, arrivedOn, completedOn));
        }

        public int Drain(out IEnumerable<Log>[] requestLogsByEndpointIndex)
        {
            lock (drainLock)
            {
                if (queue.Count > 0)
                {
                    for (int endpointIndex=0; endpointIndex<endpointsCount; endpointIndex++)
                    {
                        drainBuffers[endpointIndex].Clear();
                    }

                    var taken = 0;
                    EndpointLog log;
                    while (taken < capacity && queue.TryTake(out log))
                    {
                        drainBuffers[log.EndpointIndex].Enqueue(log);
                        taken++;
                    }

                    requestLogsByEndpointIndex = new IEnumerable<Log>[endpointsCount];
                    for (int endpointIndex=0; endpointIndex<endpointsCount; endpointIndex++)
                    {
                        requestLogsByEndpointIndex[endpointIndex] = drainBuffers[endpointIndex];
                    }
                    return taken;
                }
                else
                {
                    requestLogsByEndpointIndex = null;
                    return 0;
                }
            }
        }
    }
}
