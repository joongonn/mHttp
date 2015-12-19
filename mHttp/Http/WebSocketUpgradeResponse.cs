using System;
using System.Net;

namespace m.Http
{
    public abstract class WebSocketUpgradeResponse : HttpResponse
    {
        public sealed class AcceptUpgradeResponse : WebSocketUpgradeResponse
        {
            public readonly string RequestVersion;
            public readonly string RequestKey;
            public readonly string RequestExtensions;

            public readonly Action<IWebSocketSession> OnAccepted;

            public AcceptUpgradeResponse(string requestVersion,
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
            public RejectUpgradeResponse(HttpStatusCode reason) : base(reason) { }
        }

        protected WebSocketUpgradeResponse(HttpStatusCode statusCode) : base(statusCode) { }
    }
}
