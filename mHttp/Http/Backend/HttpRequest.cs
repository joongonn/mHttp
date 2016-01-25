using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Linq;

using m.Http;
using m.Http.Backend.WebSockets;
using m.Http.Backend.Tcp;

namespace m.Http.Backend
{
    public sealed class HttpRequest : IHttpRequest, IWebSocketUpgradeRequest
    {
        internal RequestParser.State State { get; set; }

        public IPEndPoint RemoteEndPoint { get; private set; }
        public bool IsSecureConnection { get; private set; }
        public string Host { get; internal set; }
        public Method Method { get; internal set; }
        public Uri Url { get; internal set; }
        public string Path { get; internal set; }
        public IReadOnlyDictionary<string, string> PathVariables { get; internal set; }
        public string Query { get; internal set; }
        public IReadOnlyDictionary<string, string> Headers  { get { return headers; } }
        public string ContentType { get; internal set; }
        public int ContentLength { get; internal set; }

        public bool IsKeepAlive { get; internal set; }

        public Stream Body { get; internal set; }

        readonly Dictionary<string, string> headers;

        internal HttpRequest(IPEndPoint remoteEndPoint, bool isSecureConnection) // constructed by Session-Parser
        {
            RemoteEndPoint = remoteEndPoint;
            IsSecureConnection = isSecureConnection;
            State = RequestParser.State.ReadRequestLine;

            headers = new Dictionary<string, string>(8, StringComparer.OrdinalIgnoreCase); //TODO: observable performance penalty with case insensitivity support
        }

        public HttpRequest(IPEndPoint remoteEndPoint,
                           bool isSecureConnection,
                           string host,
                           Method method,
                           Uri url,
                           string path,
                           string query,
                           Dictionary<string, string> headers,
                           string contentType,
                           int contentLength,
                           bool isKeepAlive,
                           Stream body)
        {
            RemoteEndPoint = remoteEndPoint;
            IsSecureConnection = isSecureConnection;
            State = RequestParser.State.Completed;

            Host = host;
            Method = method;
            Url = url;
            Path = path;
            Query = query;
            this.headers = headers;
            ContentType = contentType;
            ContentLength = contentLength;
            IsKeepAlive = isKeepAlive;
            Body = body;
        }

        internal void SetHeader(string name, string value)
        {
            headers[name] = value;
        }

        public string GetHeader(string nameIgnoreCase)
        {
            string value;

            if (headers.TryGetValue(nameIgnoreCase, out value))
            {
                if (value == string.Empty)
                {
                    throw new RequestException(string.Format("'{0}' header cannot be empty", nameIgnoreCase), HttpStatusCode.BadRequest);
                }

                return value;
            }
            else
            {
                throw new RequestException(string.Format("'{0}' header not found", nameIgnoreCase), HttpStatusCode.BadRequest);
            }
        }

        public string GetHeaderWithDefault(string nameIgnoreCase, string defaultValue)
        {
            string value;
            if (headers.TryGetValue(nameIgnoreCase, out value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        public T GetHeader<T>(string name)
        {
            var value = GetHeader(name);
            var converter = TypeDescriptor.GetConverter(typeof(T));

            return (T)converter.ConvertFromString(value);
        }

        public T GetHeaderWithDefault<T>(string nameIgnoreCase, T defaultValue)
        {
            string value;
            if (headers.TryGetValue(nameIgnoreCase, out value))
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter.ConvertFromString(value);
            }
            else
            {
                return defaultValue;
            }
        }

        WebSocketUpgradeResponse.AcceptUpgradeResponse IWebSocketUpgradeRequest.AcceptUpgrade(Action<IWebSocketSession> onAccepted)
        {
            string version, key, extensions;

            if (this.IsWebSocketUpgradeRequest(out version, out key, out extensions))
            {
                return new WebSocketUpgradeResponse.AcceptUpgradeResponse(version, key, extensions, onAccepted);
            }
            else
            {
                throw new RequestException("Not a websocket upgrade request", HttpStatusCode.BadRequest);
            }
        }

        WebSocketUpgradeResponse.RejectUpgradeResponse IWebSocketUpgradeRequest.RejectUpgrade(HttpStatusCode reason)
        {
            return new WebSocketUpgradeResponse.RejectUpgradeResponse(reason);
        }

        public static implicit operator HttpRequest(HttpListenerRequest req)
        {
            return new HttpRequest(req.RemoteEndPoint,
                                   req.IsSecureConnection,
                                   req.Headers.Get("Host"), //TODO: nullable?
                                   req.GetMethod(),
                                   req.Url,
                                   req.Url.AbsolutePath,
                                   req.Url.Query,
                                   req.Headers.AllKeys.ToDictionary(k => k, k => req.Headers[k], StringComparer.OrdinalIgnoreCase),
                                   req.ContentType,
                                   (int)req.ContentLength64,
                                   req.KeepAlive,
                                   req.InputStream);
        }
    }
}
