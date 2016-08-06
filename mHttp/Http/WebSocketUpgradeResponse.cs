using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using m.Http.Backend.Tcp;

namespace m.Http
{
    public abstract class WebSocketUpgradeResponse : HttpResponse
    {
        public sealed class AcceptUpgradeResponse : WebSocketUpgradeResponse
        {
            public string RequestVersion { get; }
            public string RequestKey { get; }
            public string RequestExtensions { get; }

            public Action<IWebSocketSession> OnAccepted { get; }

            internal AcceptUpgradeResponse(string requestVersion,
                                           string requestKey,
                                           string requestExtensions,
                                           Action<IWebSocketSession> onAccepted) : base(HttpStatusCode.SwitchingProtocols)
            {
                RequestVersion = requestVersion;
                RequestKey = requestKey;
                RequestExtensions = requestExtensions;
                OnAccepted = onAccepted;
            }

            internal override async Task<int> WriteToAsync(Stream toStream, int keepAlives, TimeSpan keepAliveTimeout)
            {
                var response = HttpResponseWriter.GetAcceptWebSocketUpgradeResponse((int)StatusCode, StatusDescription, RequestKey);

                try
                {
                    await toStream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
                    return response.Length;
                }
                catch (Exception e)
                {
                    throw new SessionStreamException("Exception writing to stream", e);
                }
            }
        }

        public sealed class RejectUpgradeResponse : WebSocketUpgradeResponse
        {
            internal RejectUpgradeResponse(HttpStatusCode reason) : base(reason) { }

            internal override async Task<int> WriteToAsync(Stream toStream, int keepAlives, TimeSpan keepAliveTimeout)
            {
                var response = HttpResponseWriter.GetRejectWebSocketUpgradeResponse((int)StatusCode, StatusDescription);

                try
                {
                    await toStream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
                    return response.Length;
                }
                catch (Exception e)
                {
                    throw new SessionStreamException("Exception writing to stream", e);
                }
            }
        }

        WebSocketUpgradeResponse(HttpStatusCode statusCode) : base(statusCode) { }
    }
}
