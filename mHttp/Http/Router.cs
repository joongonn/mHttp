using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using NLog;

using m.Http.Routing;
using m.Http.Metrics;
using m.Utils;

namespace m.Http
{
    public class Router : LifeCycleBase, IEnumerable<RouteTable>
    {
        static readonly HttpResponse NotFound = new ErrorResponse(HttpStatusCode.NotFound);
        static readonly HttpResponse BadRequest = new ErrorResponse(HttpStatusCode.BadRequest);

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly RouteTable[] routeTables;
        readonly RequestLogs requestLogs;
        readonly WaitableTimer timer;

        readonly RateLimitedEndpoint[] rateLimitedEndpoints;

        public RouterMetrics Metrics { get; private set; }
        public RouteTable this[int RouteTableIndex] { get { return routeTables[RouteTableIndex]; } }
        public int Length { get { return routeTables.Length; } }

        public Router(RouteTable routeTable, int requestLogsSize=8192, int timerPeriodMs=100) : this(new [] { routeTable }, requestLogsSize, timerPeriodMs) { }

        public Router(RouteTable[] routeTables, int requestLogsSize=8192, int timerPeriodMs=100)
        {
            this.routeTables = routeTables;

            rateLimitedEndpoints = routeTables.SelectMany(table => table.Where(ep => ep is RateLimitedEndpoint).Cast<RateLimitedEndpoint>())
                                              .ToArray();

            requestLogs = new RequestLogs(this, requestLogsSize);
            Metrics = new RouterMetrics(this);
            timer = new WaitableTimer("RouterTimer",
                                      TimeSpan.FromMilliseconds(timerPeriodMs),
                                      new [] {
                                          new WaitableTimer.Job("UpdateRateLimitBuckets", UpdateRateLimitBuckets),
                                          new WaitableTimer.Job("ProcessRequestLogs", ProcessRequestLogs)
                                      });
        }

        public IEnumerator<RouteTable> GetEnumerator()
        {
            return ((IEnumerable<RouteTable>)routeTables).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected override void OnStart()
        {
            timer.Start();
        }

        protected override void OnShutdown()
        {
            timer.Shutdown();
        }

        void UpdateRateLimitBuckets()
        {
            for (int i=0; i<rateLimitedEndpoints.Length; i++)
            {
                rateLimitedEndpoints[i].UpdateRateLimitBucket();
            }
        }

        void ProcessRequestLogs()
        {
            IEnumerable<RequestLogs.Log>[][] logs;
            if (requestLogs.Drain(out logs) > 0)
            {
                Metrics.Update(logs);
            }
        }

        int MatchRouteTable(string host)
        {
            for (int i=0; i<routeTables.Length; i++)
            {
                if (routeTables[i].MatchRequestedHost(host))
                {
                    return i;
                }
            }

            return -1;
        }

        public async Task<HttpResponse> HandleHttpRequest(HttpRequest httpReq, DateTime requestArrivedOn)
        {
            var requestedHost = httpReq.Host;
            if (string.IsNullOrEmpty(requestedHost))
            {
                return BadRequest;
            }

            HttpResponse httpResp;

            int routeTableIndex;
            if ((routeTableIndex = MatchRouteTable(requestedHost)) < 0)
            {
                httpResp = NotFound; // no matching host
            }
            else
            {
                RouteTable routeTable = routeTables[routeTableIndex];

                //TODO: httpReq = routeTable.FilterRequest(httpReq);

                int endpointIndex;
                IReadOnlyDictionary<string, string> urlVariables;
                if ((endpointIndex = routeTable.TryMatchEndpoint(httpReq.Method, httpReq.Url, out urlVariables)) < 0)
                {
                    httpResp = NotFound; // no matching route
                }
                else
                {
                    try
                    {
                        httpResp = await routeTable[endpointIndex].Handler(new Request(httpReq, urlVariables));
                    }
                    catch (RequestException e)
                    {
                        httpResp = new ErrorResponse(e.HttpStatusCode, e);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Error handling request:[{0}:{1}] - [{2}]: {3}", httpReq.Method, httpReq.Path, e.GetType().Name, e.Message);
                        httpResp = new ErrorResponse(HttpStatusCode.InternalServerError, e);
                    }
                }

                //TODO: httpResp = routeTable.FilterResponse(httpReq, httpResp);

                if (endpointIndex >= 0)
                {
                    while (!requestLogs.TryAdd(routeTableIndex, endpointIndex, httpReq, httpResp, requestArrivedOn, DateTime.UtcNow))
                    {
                        timer.Signal();
                        // await Task.Yield();
                    }
                }
            }

            return httpResp;
        }
    }
}
