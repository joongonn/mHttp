using System;
using System.Collections.Generic;
using System.Net;

namespace m.Http
{
    public interface IWebSocketUpgradeRequest
    {
        Uri Url { get; }
        string Path { get; }
        IReadOnlyDictionary<string, string> PathVariables { get; }
        string Query { get; }

        WebSocketUpgradeResponse.AcceptUpgradeResponse AcceptUpgrade(Action<IWebSocketSession> onAccepted);

        WebSocketUpgradeResponse.RejectUpgradeResponse RejectUpgrade(HttpStatusCode reason);
    }
}
