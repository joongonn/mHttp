using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using m.Http.Backend;
using m.Http.Backend.Tcp;
using m.Http.Metrics;
using m.Logging;
using m.Utils;

namespace m.Http
{
    public class TcpListenerBackend
    {
        readonly LoggingProvider.ILogger logger = LoggingProvider.GetLogger(typeof(TcpListenerBackend));

        readonly string name;
        readonly int maxKeepAlives;
        readonly int backlog;
        readonly int sessionReadBufferSize;
        readonly TimeSpan sessionReadTimeout;
        readonly TimeSpan sessionWriteTimeout;
        readonly TcpListener listener;
        readonly LifeCycleToken lifeCycleToken;

        readonly RateCounter sessionRate = new RateCounter(100);
        readonly ConcurrentDictionary<long, Session> sessionTable;
        readonly ConcurrentDictionary<long, long> sessionReads;
        readonly ConcurrentDictionary<long, WebSocketSession> webSocketSessionTable; //TODO: track dead reads ?

        readonly WaitableTimer timer;

        long acceptedSessions = 0;
        long acceptedWebSocketSessions = 0;
        int maxConnectedSessions = 0;
        int maxConnectedWebSocketSessions = 0;

        Router router;
        BackendMetrics metrics;

        public TcpListenerBackend(IPAddress address,
                                  int port,
                                  int maxKeepAlives=100,
                                  int backlog=128,
                                  int sessionReadBufferSize=1024,
                                  int sessionReadTimeoutMs=5000,
                                  int sessionWriteTimeoutMs=5000)
        {
            listener = new TcpListener(address, port);
            this.maxKeepAlives = maxKeepAlives;
            this.backlog = backlog;
            this.sessionReadBufferSize = sessionReadBufferSize;
            sessionReadTimeout = TimeSpan.FromMilliseconds(sessionReadTimeoutMs);
            sessionWriteTimeout = TimeSpan.FromMilliseconds(sessionWriteTimeoutMs);
            lifeCycleToken = new LifeCycleToken();
            sessionTable = new ConcurrentDictionary<long, Session>();
            sessionReads = new ConcurrentDictionary<long, long>();
            webSocketSessionTable = new ConcurrentDictionary<long, WebSocketSession>();

            name = string.Format("TcpListenerBackend({0}:{1})", address, port);

            timer = new WaitableTimer("TcpListenerBackendTimer",
                                      TimeSpan.FromSeconds(1),
                                      new [] {
                                          new WaitableTimer.Job("CheckSessionReadTimeouts", CheckSessionReadTimeouts)
                                      });
        }

        public void Start(RouteTable routeTable)
        {
            Start(new Router(routeTable));
        }

        public void Start(Router router)
        {
            metrics = new BackendMetrics(router);

            if (lifeCycleToken.Start())
            {
                timer.Start();

                this.router = router;
                this.router.Start();

                var connectionLoopThread = new Thread(ConnectionLoop)
                {
                    Priority = ThreadPriority.AboveNormal,
                    IsBackground = false,
                    Name = name
                };

                connectionLoopThread.Start();
            }
        }

        public void Shutdown()
        {
            if (lifeCycleToken.Shutdown())
            {
                timer.Shutdown();
                listener.Stop();
            }
        }

        void ConnectionLoop()
        {
            listener.Start(backlog);
            logger.Info("Listening on {0}", listener.LocalEndpoint);

            while (true)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    acceptedSessions++;
                    long sessionId = acceptedSessions;

                    Task.Run(() => HandleNewConnection(sessionId, client));
                }
                catch (SocketException e)
                {
                    if (lifeCycleToken.IsShutdown) // triggered by listener.Stop()
                    {
                        logger.Info("Listener shutting down");
                        break;
                    }
                    else
                    {
                        logger.Error("Exception while accepting TcpClient - {0}", e.ToString());
                    }
                }
            }

            logger.Info("Listener closed (accepted: {0})", acceptedSessions);

            router.Shutdown();
        }

        void HandleNewConnection(long sessionId, TcpClient client)
        {
            var stream = client.GetStream();
            var newSession = new Session(sessionId, client, stream, maxKeepAlives, sessionReadBufferSize, sessionReadTimeout, sessionWriteTimeout);

            sessionRate.Count(Time.CurrentTimeMillis, 1);

            TrackSession(newSession);
            Task.Run(() => HandleSession(newSession));
        }

        async Task HandleSession(Session session)
        {
            var closeSessionOnReturn = true;

            try
            {
                var continueSession = true;

                while (continueSession && !session.IsDisconnected())
                {
                    try
                    {
                        TrackSessionRead(session.Id);
                        if (await session.ReadToBufferAsync().ConfigureAwait(false) == 0) // 0 => client clean disconnect
                        {
                            break;
                        }
                    }
                    finally
                    {
                        UntrackSessionRead(session.Id);
                    }

                    int requestBytesParsed, responseBytesWritten;
                    HttpRequest request;
                    while (continueSession && session.TryParseNextRequestFromBuffer(out requestBytesParsed, out request))
                    {
                        Router.HandleResult result = await router.HandleRequest(request, DateTime.UtcNow).ConfigureAwait(false);
                        HttpResponse response = result.HttpResponse;

                        var webSocketUpgradeResponse = response as WebSocketUpgradeResponse;
                        if (webSocketUpgradeResponse == null)
                        {
                            responseBytesWritten = session.WriteResponse(response, request.IsKeepAlive);
                            continueSession = request.IsKeepAlive && session.KeepAlivesRemaining > 0;
                        }
                        else
                        {
                            var isUpgraded = HandleWebsocketUpgrade(session,
                                                                    result.MatchedRouteTableIndex,
                                                                    result.MatchedEndpointIndex,
                                                                    webSocketUpgradeResponse,
                                                                    out responseBytesWritten);
                            continueSession = false;
                            closeSessionOnReturn = !isUpgraded;
                        }

                        if (result.MatchedRouteTableIndex >= 0 && result.MatchedEndpointIndex >= 0)
                        {
                            metrics.CountBytes(result.MatchedRouteTableIndex, result.MatchedEndpointIndex, requestBytesParsed, responseBytesWritten);
                        }
                    }
                }
            }
            catch (RequestException e)
            {
                logger.Warn("Error parsing or bad request - {0}", e.Message);
            }
            catch (SessionStreamException)
            {
                // forced disconnect, socket errors
            }
            catch (Exception e)
            {
                logger.Fatal("Internal server error handling session - {0}", e.ToString());
            }
            finally
            {
                UntrackSession(session.Id);
                if (closeSessionOnReturn)
                {
                    session.CloseQuiety();
                }
            }
        }

        bool HandleWebsocketUpgrade(Session session,
                                    int routeTableIndex,
                                    int endpointIndex,
                                    WebSocketUpgradeResponse response,
                                    out int responseBytesWritten)
        {
            responseBytesWritten = session.WriteWebSocketUpgradeResponse(response);

            var acceptUpgradeResponse = response as WebSocketUpgradeResponse.AcceptUpgradeResponse;
            if (acceptUpgradeResponse == null)
            {
                return false;
            }
            else
            {
                long id = Interlocked.Increment(ref acceptedWebSocketSessions);
                var webSocketSession = new WebSocketSession(id,
                                                            session.TcpClient,
                                                            session.Stream,
                                                            bytesReceived => metrics.CountRequestBytesIn(routeTableIndex, endpointIndex, bytesReceived),
                                                            bytesSent => metrics.CountResponseBytesOut(routeTableIndex, endpointIndex, bytesSent),
                                                            () => UntrackWebSocketSession(id));
                TrackWebSocketSession(webSocketSession);

                try
                {
                    acceptUpgradeResponse.OnAccepted(webSocketSession); //TODO: Task.Run this?
                    return true;
                }
                catch (Exception e)
                {
                    UntrackWebSocketSession(id);
                    logger.Error("Error calling WebSocketUpgradeResponse.OnAccepted callback - {0}", e.ToString());
                    return false;
                }
            }
        }

        void TrackSession(Session session)
        {
            sessionTable[session.Id] = session;
            var sessionCount = sessionTable.Count;

            int currentMax;
            while ((currentMax = maxConnectedSessions) < sessionCount)
            {
                if (Interlocked.CompareExchange(ref maxConnectedSessions, sessionCount, currentMax) != currentMax)
                {
                    continue;
                }
            }
        }

        void UntrackSession(long id)
        {
            Session _;
            sessionTable.TryRemove(id, out _);
        }

        void TrackSessionRead(long id)
        {
            sessionReads[id] = Time.CurrentTimeMillis;
        }

        void UntrackSessionRead(long id)
        {
            long _;
            sessionReads.TryRemove(id, out _);
        }

        void TrackWebSocketSession(WebSocketSession session)
        {
            webSocketSessionTable[session.Id] = session;

            var sessionCount = webSocketSessionTable.Count;
            int currentMax;
            while ((currentMax = maxConnectedWebSocketSessions) < sessionCount)
            {
                if (Interlocked.CompareExchange(ref maxConnectedWebSocketSessions, sessionCount, currentMax) != currentMax)
                {
                    continue;
                }
            }
        }

        void UntrackWebSocketSession(long id)
        {
            WebSocketSession _;
            webSocketSessionTable.TryRemove(id, out _);
        }

        void CheckSessionReadTimeouts()
        {
            var now = Time.CurrentTimeMillis;

            foreach (var kvp in sessionReads)
            {
                if (now - kvp.Value > sessionReadTimeout.TotalMilliseconds)
                {
                    sessionTable[kvp.Key].CloseQuiety();
                }
            }
        }

        public object GetMetricsReport() //TODO: typed report
        {
            if (!lifeCycleToken.IsStarted)
            {
                throw new InvalidOperationException("Not started");
            }

            Thread.MemoryBarrier();

            return new
            {
                Time = DateTime.UtcNow.ToString(Time.StringFormat),
                Backend = new {
                    Sessions = new {
                        CurrentRate = sessionRate.GetCurrentRate(),
                        Current = sessionTable.Count,
                        Max = maxConnectedSessions,
                        Total = acceptedSessions
                    },
                    WebSocketSessions = new {
                        Current = webSocketSessionTable.Count,
                        Max = maxConnectedWebSocketSessions,
                        Total = acceptedWebSocketSessions
                    }
                },
                HostReports = HostReport.Generate(router, router.Metrics, metrics)
            };
        }
    }
}
