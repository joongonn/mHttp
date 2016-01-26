using System;

namespace m.Http.Backend.WebSockets
{
    static class Extensions
    {
        public static bool IsWebSocketUpgradeRequest(this HttpRequest req,
                                                     out string webSocketVersion,
                                                     out string webSocketKey,
                                                     out string webSocketExtensions)
        {
            var connection = req.GetHeaderWithDefault(HttpHeader.Connection, null);

            if (connection?.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var upgrade = req.GetHeaderWithDefault(HttpHeader.Upgrade, null);
                if (string.Equals(upgrade, "websocket", StringComparison.OrdinalIgnoreCase))
                {
                    webSocketVersion = req.GetHeader(HttpHeader.WebSocketVersion);
                    webSocketKey = req.GetHeader(HttpHeader.WebSocketKey);
                    webSocketExtensions = req.GetHeader(HttpHeader.WebSocketExtensions);
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
