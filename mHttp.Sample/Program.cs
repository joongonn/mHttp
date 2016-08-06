using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using m.Config;
using m.Http;
using m.Logging;

namespace m.Sample
{
    class WebSocketService
    {
        readonly LoggingProvider.ILogger logger = LoggingProvider.GetLogger(typeof(WebSocketService));

        readonly HttpResponse Index = new RedirectResponse("/index.html");
        readonly ConcurrentDictionary<long, IWebSocketSession> sessions;

        public WebSocketService()
        {
            sessions = new ConcurrentDictionary<long, IWebSocketSession>();
        }

        public HttpResponse Redirect(IHttpRequest req)
        {
            string userAgent;
            req.Headers.TryGetValue(HttpHeader.UserAgent, out userAgent);
            logger.Debug("Incoming request for index from:[{0}] with useragent:[{1}]", req.RemoteEndPoint, userAgent);
            return Index;
        }

        public WebSocketUpgradeResponse HandleUpgradeRequest(IWebSocketUpgradeRequest upgradeRequest)
        {
            return upgradeRequest.AcceptUpgrade(session => Task.Run(() => HandleWebSocketSession(session)));
        }

        async Task HandleWebSocketSession(IWebSocketSession session)
        {
            sessions[session.Id] = session;

            try
            {
                using (session)
                {
                    BroadcastMessage(string.Format("*** session-{0} connected ({1} online)", session.Id, sessions.Count));

                    while (session.IsOpen)
                    {
                        var message = await session.ReadNextMessageAsync().ConfigureAwait(false);

                        switch (message.MessageType)
                        {
                            case WebSocketMessage.Type.Text:
                                var text = (WebSocketMessage.Text)message;
                                BroadcastMessage(string.Format("<session-{0}> {1}", session.Id, text.Payload));
                                break;

                            case WebSocketMessage.Type.Close:
                                session.CloseSession();
                                break;

                            case WebSocketMessage.Type.Ping:
                                session.SendPong();
                                break;

                            case WebSocketMessage.Type.Pong:
                                break;
                        }
                    }
                }
            }
            finally
            {
                sessions.TryRemove(session.Id, out session);
                BroadcastMessage(string.Format("*** session-{0} disconnected", session.Id));
            }
        }

        void BroadcastMessage(string message)
        {
            try
            {
                foreach (var s in sessions.Values)
                {
                    s.SendText(message);
                }
            }
            catch
            {
                return;
            }
        }
    }

    class Program
    {
        class ServerConfig : IConfigurable
        {
            [EnvironmentVariable("httpListenPort")]
            public int ListenPort { get; set; } = 8080; // Default
        }

        static readonly HttpResponse greeting = new TextResponse("Hello, World");

        static async Task<HttpResponse> DelayedGreeter()
        {
            await Task.Delay(1000);
            return greeting;
        }

        public static void Main(string[] args)
        {
            var procs = Environment.ProcessorCount;
            ThreadPool.SetMaxThreads(procs, Math.Max(1, procs / 4));
            ThreadPool.SetMaxThreads(procs * 2, Math.Max(1, procs / 2));
            
            LoggingProvider.Use(LoggingProvider.ConsoleLoggingProvider);

            var config = ConfigManager.Load<ServerConfig>();

            var server = new HttpBackend(IPAddress.Any, config.ListenPort);

            var wsService = new WebSocketService();
            var staticWebContentFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web");

            var routeTable = new RouteTable(
                Route.Get("/*").With(new DirectoryInfo(staticWebContentFolder)),
                Route.Get("/").With(wsService.Redirect),
                Route.Get("/plaintext").With(() => greeting),
                Route.Get("/plaintext/delayed").WithAsync(DelayedGreeter),
                Route.GetWebSocketUpgrade("/ws").With(wsService.HandleUpgradeRequest),
                Route.Get("/metrics").With(Lift.ToJsonHandler(server.GetMetricsReport))
                                     .ApplyResponseFilter(Filters.GZip)
                                     .LimitRate(100)
            );

            server.Start(routeTable);
        }
    }
}
