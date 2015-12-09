using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using NLog;

using m.Http.Backend.Tcp;
using m.Utils;

namespace m.Http
{
    public class TcpListenerBackend
    {
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        readonly string name;
        readonly int maxKeepAlives;
        readonly int backlog;
        readonly int sessionReadBufferSize;
        readonly TimeSpan sessionReadTimeout;
        readonly TimeSpan sessionWriteTimeout;
        readonly TcpListener listener;
        readonly LifeCycleToken lifeCycleToken;

        readonly ConcurrentDictionary<long, Session> sessionTable;
        readonly ConcurrentDictionary<long, long> sessionReads;

        readonly WaitableTimer timer;

        Router router;

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
            if (lifeCycleToken.Start())
            {
                timer.Start();

                this.router = router;
                this.router.Start();

                var acceptorLoopThread = new Thread(AcceptorLoop)
                {
                    Priority = ThreadPriority.AboveNormal,
                    IsBackground = false,
                    Name = name
                };

                acceptorLoopThread.Start();
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

        void AcceptorLoop()
        {
            listener.Start(backlog);
            logger.Info("Listening on {0}", listener.LocalEndpoint);

            long accepted = 0;

            while (true)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    var clientId = ++accepted;

                    Task.Run(() => HandleSession(new Session(clientId, client, maxKeepAlives, sessionReadBufferSize, sessionReadTimeout, sessionWriteTimeout)));
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

            logger.Info("Listener closed (accepted: {0})", accepted);

            router.Shutdown();
        }

        void CheckSessionReadTimeouts()
        {
            var now = Time.CurrentTimeMillis;

            foreach (var kvp in sessionReads)
            {
                if (now - kvp.Value > sessionReadTimeout.TotalMilliseconds)
                {
                    sessionTable[kvp.Key].Close();
                }
            }
        }

        async Task HandleSession(Session session)
        {
            sessionTable[session.Id] = session;

            try
            {
                while (!session.IsDisconnected())
                {
                    try
                    {
                        sessionReads[session.Id] = Time.CurrentTimeMillis;

                        if (await session.ReadToBufferAsync() == 0) 
                        {
                            break; // client clean disconnect
                        }
                    }
                    finally
                    {
                        long _;
                        sessionReads.TryRemove(session.Id, out _);
                    }

                    IHttpRequest parsedRequest;
                    if (session.TryParseRequestFromBuffer(out parsedRequest))
                    {
                        var request = parsedRequest as HttpRequest;
                        if (request != null)
                        {
                            HttpResponse response = await router.HandleHttpRequest(request, DateTime.UtcNow).ConfigureAwait(false);

                            session.WriteResponse(response, request.IsKeepAlive);

                            if (!request.IsKeepAlive || session.KeepAlivesRemaining == 0)
                            {
                                break;
                            }
                        }
                        else
                        {
                            //TODO: websocket
                            // var webSocketRequest = parsedRequest as HttpWebSocketRequest;

                            // Task.Run(() => HandleWebSocketSession(session, webSocketRequest));

                            break;
                        }
                    }
                }
            }
            catch (ParseRequestException e)
            {
                logger.Warn("Error parsing (bad) http request - {0}", e.Message);
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
                sessionTable.TryRemove(session.Id, out session);
            }

            session.Close();
        }

        public HttpResponse GetMetricsReport()
        {
            if (!lifeCycleToken.IsStarted)
            {
                throw new InvalidOperationException("Not started");
            }

            return new JsonResponse(new {
                ConnectedSessions = sessionTable.Count,
                Reports = router.Metrics.GetReports(),
            });
        }
    }
}
