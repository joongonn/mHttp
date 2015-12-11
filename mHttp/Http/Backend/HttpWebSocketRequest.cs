using System;
using System.Collections.Generic;

namespace m.Http.Backend
{
    public sealed class HttpWebSocketRequest : IRequest
    {
        public IReadOnlyDictionary<string, string> Headers { get; private set; }

        public string WebSocketVersion { get; private set; }
        public string WebSocketKey { get; private set; }
        public string WebSocketExtensions { get; private set; }

        public Uri Url { get; private set; }
        public string Path { get; private set; }
        public string Query { get; private set; }

        public HttpWebSocketRequest(IReadOnlyDictionary<string, string> headers,
                                    string webSocketVersion,
                                    string webSocketKey,
                                    string webSocketExtensions,
                                    Uri url)
        {
            Headers = headers;
            WebSocketVersion = webSocketVersion;
            WebSocketKey = webSocketKey;
            WebSocketExtensions = webSocketExtensions;
            Url = url;
        }
    }
}
