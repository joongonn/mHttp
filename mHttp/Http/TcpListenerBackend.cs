using System;
using System.Collections.Concurrent;
using System.IO;
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

                    Task.Run(() => HandleSession(new Session(clientId, client, sessionWriteTimeout)));
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

            var buffer = new byte[sessionReadBufferSize];
            int start = 0, bufferOffset = 0, requests = 0;
            var state = new RequestState();
            var keepAlives = maxKeepAlives;

            try
            {
                while (!session.IsDisconnected())
                {
                    int bufferRemaining = buffer.Length - bufferOffset;
                    if (bufferRemaining == 0)
                    {
                        var newBuffer = new byte[buffer.Length * 2];
                        Array.Copy(buffer, newBuffer, buffer.Length);
                        buffer = newBuffer;
                        continue;
                    }

                    int bytesRead;
                    try
                    {
                        sessionReads[session.Id] = Time.CurrentTimeMillis;
                        bytesRead = await session.ReadAsync(buffer, bufferOffset, bufferRemaining); // returns 0 on clean disconnect
                    }
                    catch // socket closed
                    {
                        break;
                    }
                    finally
                    {
                        long timeStartedIgnore;
                        sessionReads.TryRemove(session.Id, out timeStartedIgnore);
                    }

                    if (bytesRead > 0) // else client disconnected
                    {
                        bufferOffset += bytesRead;

                        bool isParsed;
                        HttpRequest httpRequest;
                        try
                        {
                            isParsed = RequestParser.TryParseHttpRequest(buffer, ref start, bufferOffset, state, out httpRequest);
                            requests++;
                        }
                        catch (RequestException e)
                        {
                            logger.Warn("Error parsing (bad) http request - {0}", e.Message);
                            break;
                        }

                        if (isParsed)
                        {
                            start = 0;
                            bufferOffset = 0;
                            state = new RequestState();

                            HttpResponse httpResponse = await router.HandleHttpRequest(httpRequest, DateTime.UtcNow);

                            var response = new MemoryStream(1024 + httpResponse.Body.Length);
                            HttpResponseWriter.WriteResponse(httpResponse, response, httpRequest.IsKeepAlive ? keepAlives : 0, sessionReadTimeout);

                            try
                            {
                                session.Write(response.GetBuffer(), 0, (int)response.Length);
                            }
                            catch
                            {
                                break;
                            }

                            keepAlives--;
                            if (httpRequest.IsKeepAlive && keepAlives >= 0)
                            {
                                continue;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Fatal("Internal server error handling session - {0}", e.ToString());
            }

            session.Close();

            sessionTable.TryRemove(session.Id, out session);
        }

        public HttpResponse GetMetricsReport()
        {
            if (!lifeCycleToken.IsStarted)
            {
                throw new InvalidOperationException("Not started");
            }

            return new JsonResponse(new {
                ConnectedSessions = sessionTable.Count,
                Endpoints = router.Metrics.GetReport().Endpoints,
            });
        }
    }
}
