using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NLog;

using m.Utils;

namespace m.Http
{
    public sealed class HttpListenerBackend
    {
        readonly HttpResponse ServiceUnavailable = new ErrorResponse(HttpStatusCode.ServiceUnavailable);

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly string ListenOn;
        readonly string name;
        readonly HttpListener listener;
        readonly Semaphore getContexts;
        readonly LifeCycleToken lifeCycleToken;

        Router router;

        public HttpListenerBackend(string listenAddress, int listenPort)
        {
            ListenOn = string.Format("http://{0}:{1}/", listenAddress, listenPort);
            name = string.Format("HttpListenerBackend({0}:{1})", listenAddress, listenPort);
            listener = new HttpListener
            {
                IgnoreWriteExceptions = true,
            };
            listener.Prefixes.Add(ListenOn);
            getContexts = new Semaphore(Environment.ProcessorCount, Environment.ProcessorCount);

            lifeCycleToken = new LifeCycleToken();
        }

        public void Start(RouteTable routeTable)
        {
            Start(new Router(routeTable));
        }

        public void Start(Router router)
        {
            if (lifeCycleToken.Start())
            {
                this.router = router;
                this.router.Start();

                var getContextLoopThread = new Thread(GetContextLoop)
                {
                    Priority = ThreadPriority.AboveNormal,
                    IsBackground = false,
                    Name = name
                };

                getContextLoopThread.Start();
            }
        }

        public void Shutdown()
        {
            lifeCycleToken.Shutdown();
        }

        void GetContextLoop()
        {
            listener.Start();
            logger.Info("Listening on [{0}]", ListenOn);

            while (!lifeCycleToken.IsShutdown)
            {
                if (getContexts.WaitOne(10))
                {
                    listener.GetContextAsync().ContinueWith(ProcessHttpListenerContext);
                }
            }

            listener.Stop();
            listener.Close();

            logger.Info("Listener closed.");

            router.Shutdown();
        }

        async Task ProcessHttpListenerContext(Task<HttpListenerContext> gottenContext)
        {
            var requestArrivedOn = DateTime.UtcNow;
            getContexts.Release();

            HttpListenerContext ctx = gottenContext.Result;
            HttpResponse httpResp;

            if (!lifeCycleToken.IsShutdown)
            {
                try
                {
                    HttpRequest httpReq = ctx.Request;
                    httpResp = await router.HandleHttpRequest(httpReq, requestArrivedOn);
                }
                catch (Exception e)
                {
                    logger.Fatal("InternalServerError processing HttpListenerContext - {0}", e.ToString());
                    httpResp = new ErrorResponse(HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                httpResp = ServiceUnavailable;
            }

            WriteAndCloseResponse(httpResp, ctx.Response);
        }

        static void WriteAndCloseResponse(HttpResponse httpResp, HttpListenerResponse respCtx)
        {
            try
            {
                respCtx.StatusCode = (int)httpResp.StatusCode;
                respCtx.StatusDescription = httpResp.StatusDescription;
                respCtx.ContentType = httpResp.ContentType;

                if (httpResp.Headers != null && httpResp.Headers.Count > 0)
                {
                    foreach (var kvp in httpResp.Headers)
                    {
                        respCtx.Headers[kvp.Key] = kvp.Value;
                    }
                }

                var body = httpResp.Body;
                if (body != null && body.Length > 0)
                {
                    respCtx.ContentLength64 = body.Length;
                    respCtx.OutputStream.Write(body, 0, body.Length);
                    respCtx.OutputStream.Flush();
                    respCtx.OutputStream.Close();
                }

                respCtx.Close();
            }
            catch (Exception)
            {
                return;
            }
        }

        public HttpResponse GetMetricsReport()
        {
            if (!lifeCycleToken.IsStarted)
            {
                throw new InvalidOperationException("Not started");
            }

            return new JsonResponse(router.Metrics.GetReports());
        }
    }
}
