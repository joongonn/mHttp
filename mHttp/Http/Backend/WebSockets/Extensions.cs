using System;
using System.Net.Sockets;

namespace m.Http.Backend.WebSockets
{
    static class Extensions
    {
        public static bool IsWebSocketUpgradeRequest(this HttpRequest req,
                                                     out string webSocketVersion,
                                                     out string webSocketKey,
                                                     out string webSocketExtensions)
        {
            var connection = req.GetHeaderWithDefault(Headers.Connection, null);

            if (connection != null &&
                connection.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var upgrade = req.GetHeaderWithDefault(Headers.Upgrade, null);
                if (string.Equals(upgrade, "websocket", StringComparison.OrdinalIgnoreCase))
                {
                    webSocketVersion = req.GetHeader(Headers.WebSocketVersion);
                    webSocketKey = req.GetHeader(Headers.WebSocketKey);
                    webSocketExtensions = req.GetHeader(Headers.WebSocketExtensions);
                    return true;
                }
                else
                {
                    //TODO: throw new NotSupportedException
                }
            }

            webSocketVersion = null;
            webSocketKey = null;
            webSocketExtensions = null;
            return false;
        }
    }
}
