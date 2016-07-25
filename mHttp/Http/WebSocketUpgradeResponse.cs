using System;
using System.Net;

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
        }

        public sealed class RejectUpgradeResponse : WebSocketUpgradeResponse
        {
            internal RejectUpgradeResponse(HttpStatusCode reason) : base(reason) { }
        }

        WebSocketUpgradeResponse(HttpStatusCode statusCode) : base(statusCode) { }
    }
}
