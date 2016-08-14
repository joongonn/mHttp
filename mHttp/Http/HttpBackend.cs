using System;
using System.Collections.Concurrent;
using System.Linq;
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
    public class HttpBackend
    {
        protected readonly LoggingProvider.ILogger logger = LoggingProvider.GetLogger(typeof(HttpBackend));

        readonly int port;
        readonly string name;
        readonly int backlog;

        protected readonly int sessionReadBufferSize;
        protected readonly TimeSpan sessionReadTimeout;
        protected readonly TimeSpan sessionWriteTimeout;
        protected readonly int maxKeepAlives;

        readonly TcpListener listener;
        readonly LifeCycleToken lifeCycleToken;

        readonly RateCounter sessionRate = new RateCounter(100);
        readonly ConcurrentDictionary<long, HttpSession> sessionTable;
        readonly ConcurrentDictionary<long, long> sessionReads;
        readonly ConcurrentDictionary<long, WebSocketSession> webSocketSessionTable; //TODO: track dead reads ?

        readonly WaitableTimer timer;

        long acceptedSessions = 0;
        long acceptedWebSocketSessions = 0;
        int maxConnectedSessions = 0;
        int maxConnectedWebSocketSessions = 0;

        readonly CountingDictionary<Type> sessionExceptionCounters;

        Router router;

        public HttpBackend(IPAddress address,
                           int port,
                           int maxKeepAlives=100,
                           int backlog=128,
                           int sessionReadBufferSize=4096,
                           int sessionReadTimeoutMs=5000,
                           int sessionWriteTimeoutMs=5000)
        {
            this.port = port;
            listener = new TcpListener(address, port);
            this.maxKeepAlives = maxKeepAlives;
            this.backlog = backlog;
            this.sessionReadBufferSize = sessionReadBufferSize;
            sessionReadTimeout = TimeSpan.FromMilliseconds(sessionReadTimeoutMs);
            sessionWriteTimeout = TimeSpan.FromMilliseconds(sessionWriteTimeoutMs);
            lifeCycleToken = new LifeCycleToken();
            sessionTable = new ConcurrentDictionary<long, HttpSession>();
            sessionReads = new ConcurrentDictionary<long, long>();
            webSocketSessionTable = new ConcurrentDictionary<long, WebSocketSession>();

            sessionExceptionCounters = new CountingDictionary<Type>();

            name = string.Format("{0}({1}:{2})", GetType().Name, address, port);

            timer = new WaitableTimer(name,
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
                    var sessionId = ++acceptedSessions;

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

        async Task HandleNewConnection(long sessionId, TcpClient client)
        {
            HttpSession newSession;
            try
            {
                //TODO: configurable
                client.NoDelay = true;
                client.SendBufferSize = 8192;
                client.ReceiveBufferSize = 8192;

                newSession = await CreateSession(sessionId, client).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Warn("Error creating session - {0}", e);
                client.Close();
                return;
            }

            sessionRate.Count(Time.CurrentTimeMillis, 1);
                
            TrackSession(newSession);

            await HandleSession(newSession).ConfigureAwait(false);
        }

        internal virtual Task<HttpSession> CreateSession(long sessionId, TcpClient client)
        {
            return Task.FromResult(new HttpSession(sessionId, client, client.GetStream(), false, maxKeepAlives, sessionReadBufferSize, (int)sessionReadTimeout.TotalMilliseconds, (int)sessionWriteTimeout.TotalMilliseconds));
        }

        async Task HandleSession(HttpSession session)
        {
            var closeSessionOnReturn = true;

            try
            {
                var continueRequestLoop = true;

                while (continueRequestLoop && !session.IsDisconnected()) // request loop
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

                    while (continueRequestLoop && session.TryParseNextRequestFromBuffer(out requestBytesParsed, out request))
                    {
                        Router.HandleResult result = await router.HandleRequest(request, DateTime.UtcNow).ConfigureAwait(false);
                        HttpResponse response = result.HttpResponse;

                        if (response is WebSocketUpgradeResponse)
                        {
                            continueRequestLoop = false;

                            var acceptUpgrade = response as WebSocketUpgradeResponse.AcceptUpgradeResponse;
                            if (acceptUpgrade == null)
                            {
                                var rejectUpgrade = (WebSocketUpgradeResponse.RejectUpgradeResponse)response;
                                responseBytesWritten = await rejectUpgrade.WriteToAsync(session.Stream, 0, sessionReadTimeout).ConfigureAwait(false);
                            }
                            else
                            {
                                responseBytesWritten = await AcceptWebSocketUpgrade(session, result.MatchedRouteTableIndex, result.MatchedEndpointIndex, acceptUpgrade).ConfigureAwait(false);
                                closeSessionOnReturn = false;
                            }
                        }
                        else
                        {
                            responseBytesWritten = await response.WriteToAsync(session.Stream, request.IsKeepAlive ? session.KeepAlivesRemaining : 0, sessionReadTimeout).ConfigureAwait(false);
                            continueRequestLoop = request.IsKeepAlive && session.KeepAlivesRemaining > 0;
                        }

                        if (result.MatchedRouteTableIndex >= 0 && result.MatchedEndpointIndex >= 0)
                        {
                            router.Metrics.CountBytes(result.MatchedRouteTableIndex, result.MatchedEndpointIndex, requestBytesParsed, responseBytesWritten);
                        }
                    }
                }
            }
            catch (RequestException e)
            {
                sessionExceptionCounters.Count(e.GetType());
                logger.Warn("Error parsing or bad request - {0}", e.Message);
            }
            catch (SessionStreamException e)
            {
                sessionExceptionCounters.Count(e.GetType());
            }
            catch (Exception e)
            {
                sessionExceptionCounters.Count(e.GetType());
                logger.Fatal("Error handling session - {0}", e.ToString());
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

        async Task<int> AcceptWebSocketUpgrade(HttpSession session,
                                               int routeTableIndex,
                                               int endpointIndex,
                                               WebSocketUpgradeResponse.AcceptUpgradeResponse response)
        {
            var bytesWritten = await response.WriteToAsync(session.Stream, 0, sessionReadTimeout).ConfigureAwait(false);

            var id = Interlocked.Increment(ref acceptedWebSocketSessions);
            var webSocketSession = new WebSocketSession(id,
                                                        session.TcpClient,
                                                        session.Stream,
                                                        bytesReceived => router.Metrics.CountRequestBytesIn(routeTableIndex, endpointIndex, bytesReceived),
                                                        bytesSent => router.Metrics.CountResponseBytesOut(routeTableIndex, endpointIndex, bytesSent),
                                                        () => UntrackWebSocketSession(id),
                                                        sessionReadBufferSize,
                                                        (int)sessionReadTimeout.TotalMilliseconds,
                                                        (int)sessionWriteTimeout.TotalMilliseconds);
            TrackWebSocketSession(webSocketSession);

            try
            {
                response.OnAccepted(webSocketSession); //TODO: Task.Run this?
                return bytesWritten;
            }
            catch (Exception)
            {
                UntrackWebSocketSession(id);
                throw;
            }
        }

        void TrackSession(HttpSession session)
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
            HttpSession _;
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

            var now = DateTime.UtcNow;
            return new
            {
                Time = now.ToString(Time.StringFormat),
                TimeHours = now.ToTimeHours(),
                Backend = new {
                    Port = port,
                    Sessions = new {
                        CurrentRate = sessionRate.GetCurrentRate(),
                        MaxRate = sessionRate.MaxRate,
                        Current = sessionTable.Count,
                        Max = maxConnectedSessions,
                        Total = acceptedSessions,
                        Errors = sessionExceptionCounters.ToDictionary(kvp => kvp.Key.FullName, kvp => kvp.Value.Count)
                    },
                    WebSocketSessions = new {
                        Current = webSocketSessionTable.Count,
                        Max = maxConnectedWebSocketSessions,
                        Total = acceptedWebSocketSessions
                    }
                },
                HostReports = HostReport.Generate(router, router.Metrics)
            };
        }
    }
}
