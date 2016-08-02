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

            internal override async Task<int> WriteToAsync(Stream stream, int keepAlives, TimeSpan keepAliveTimeout)
            {
                var response = HttpResponseWriter.GetAcceptWebSocketUpgradeResponse((int)StatusCode, StatusDescription, RequestKey);
                int bytesWritten = response.Length;

                try
                {
                    await stream.WriteAsync(response, 0, bytesWritten);
                }
                catch (Exception e)
                {
                    throw new SessionStreamException("Exception while writing to session stream", e);
                }

                return bytesWritten;
            }
        }

        public sealed class RejectUpgradeResponse : WebSocketUpgradeResponse
        {
            internal RejectUpgradeResponse(HttpStatusCode reason) : base(reason) { }

            internal override async Task<int> WriteToAsync(Stream stream, int keepAlives, TimeSpan keepAliveTimeout)
            {
                var response = HttpResponseWriter.GetRejectWebSocketUpgradeResponse((int)StatusCode, StatusDescription);
                int bytesWritten = response.Length;

                try
                {
                    await stream.WriteAsync(response, 0, bytesWritten);
                }
                catch (Exception e)
                {
                    throw new SessionStreamException("Exception while writing to session stream", e);
                }

                return bytesWritten;
            }
        }

        WebSocketUpgradeResponse(HttpStatusCode statusCode) : base(statusCode) { }
    }
}
