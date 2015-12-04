using System;
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
    public class Router : LifeCycleBase
    {
        static readonly HttpResponse NotFound = new ErrorResponse(HttpStatusCode.NotFound);

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly RouteTable routeTable;
        readonly RequestLogs requestLogs;
        readonly WaitableTimer timer;

        public ServerMetrics Metrics { get; private set; }

        readonly RateLimitedEndpoint[] rateLimitedEndpoints;

        public Router(RouteTable routeTable, int requestLogsSize=8192, int timerPeriodMs=100)
        {
            this.routeTable = routeTable;

            rateLimitedEndpoints = routeTable.Where(ep => ep is RateLimitedEndpoint)
                                             .Cast<RateLimitedEndpoint>()
                                             .ToArray();

            requestLogs = new RequestLogs(routeTable.Length, requestLogsSize);
            Metrics = new ServerMetrics(routeTable);
            timer = new WaitableTimer("RouterTimer",
                                      TimeSpan.FromMilliseconds(timerPeriodMs),
                                      new [] {
                                          new WaitableTimer.Job("UpdateRateLimitBuckets", UpdateRateLimitBuckets),
                                          new WaitableTimer.Job("ProcessRequestLogs", ProcessRequestLogs)
                                      });
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
            IEnumerable<RequestLogs.Log>[] logsByEndpointIndex;
            if (requestLogs.Drain(out logsByEndpointIndex) > 0)
            {
                Metrics.Update(logsByEndpointIndex);
            }
        }

        public async Task<HttpResponse> HandleHttpRequest(HttpRequest httpReq, DateTime requestArrivedOn)
        {
            int endpointIndex;
            IReadOnlyDictionary<string, string> urlVariables;
            HttpResponse httpResp;

            //TODO: httpReq = FilterRequest(httpReq);

            if ((endpointIndex = routeTable.TryMatchEndpoint(httpReq.Method,
                                                             httpReq.Url,
                                                             out urlVariables)) >= 0)
            {
                Endpoint endpoint = routeTable[endpointIndex];
                try
                {
                    httpResp = await endpoint.Handler(new Request(httpReq, urlVariables));
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
            else
            {
                httpResp = NotFound;
            }

            //TODO: httpResp = FilterResponse(httpReq, httpResp);

            if (endpointIndex >= 0)
            {
                while (!requestLogs.TryAdd(endpointIndex, httpReq, httpResp, requestArrivedOn, DateTime.UtcNow))
                {
                    timer.Signal();
                }
            }

            return httpResp;
        }
    }
}
