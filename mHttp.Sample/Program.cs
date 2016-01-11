using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

using m.Config;
using m.Http;
using m.Logging;
using m.Http.Extensions;

namespace m.Sample
{
    class WebSocketService
    {
        readonly HttpResponse Index = new RedirectResponse("/web/index.html");
        readonly ConcurrentDictionary<long, IWebSocketSession> sessions;

        public WebSocketService()
        {
            sessions = new ConcurrentDictionary<long, IWebSocketSession>();
        }

        public HttpResponse Redirect()
        {
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
                        var message = await session.ReadNextMessageAsync();

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
            public int ListenPort { get; set; }

            public ServerConfig()
            {
                ListenPort = 8080; // Default
            }
        }

        public static void Main(string[] args)
        {
            LoggingProvider.Use(LoggingProvider.ConsoleLoggingProvider);

            var config = ConfigManager.Load<ServerConfig>();

            var wsService = new WebSocketService();

            var server = new HttpBackend(IPAddress.Any, config.ListenPort);
            var publicRouteTable = new RouteTable(
                Route.ServeDirectory("/web/*", "/web/"),
                Route.Get("/").With(wsService.Redirect),
                Route.GetWebSocketUpgrade("/ws").With(wsService.HandleUpgradeRequest),
                Route.Get("/metrics").With(Lift.ToJsonHandler(server.GetMetricsReport)
                                               .FilterResponse(Filters.GZip))
                                     .LimitRate(100)
            );

            server.Start(publicRouteTable);
        }
    }
}
