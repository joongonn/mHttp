using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using m.Http.Backend;
using m.Http.Metrics;
using m.Logging;
using m.Utils;

namespace m.Http
{
    [Obsolete]
    public sealed class BCLHttpListenerBackend
    {
        readonly LoggingProvider.ILogger logger = LoggingProvider.GetLogger(typeof(BCLHttpListenerBackend));

        readonly HttpResponse ServiceUnavailable = new ErrorResponse(HttpStatusCode.ServiceUnavailable);

        readonly string ListenOn;
        readonly string name;
        readonly HttpListener listener;
        readonly Semaphore getContexts;
        readonly LifeCycleToken lifeCycleToken;

        Router router;

        public BCLHttpListenerBackend(string listenAddress, int listenPort)
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
                    var result = await router.HandleRequest(httpReq, requestArrivedOn).ConfigureAwait(false);
                    httpResp = result.HttpResponse;
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

            await WriteAndCloseResponse(httpResp, ctx.Response).ConfigureAwait(false);
        }

        static async Task WriteAndCloseResponse(HttpResponse httpResp, HttpListenerResponse respCtx)
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
                if (body.Length > 0)
                {
                    respCtx.ContentLength64 = body.Length;
                    await body.WriteToAsync(respCtx.OutputStream).ConfigureAwait(false);
                    respCtx.OutputStream.Flush();
                    respCtx.OutputStream.Close();
                }

                respCtx.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        public object GetMetricsReport() //TODO: typed report
        {
            if (!lifeCycleToken.IsStarted)
            {
                throw new InvalidOperationException("Not started");
            }

            return HostReport.Generate(router, router.Metrics);
        }
    }
}
