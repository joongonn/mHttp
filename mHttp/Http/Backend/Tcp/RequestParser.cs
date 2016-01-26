using System;
using System.Linq;
using System.Net;
using System.Text;

using m.Utils;

namespace m.Http.Backend.Tcp
{
    static class RequestParser
    {
        public enum State
        {
            ReadRequestLine,
            ReadHeaders,
            ReadBodyToEnd,
            Completed
        }

        const byte SP = 32;
        const byte COLON = (byte)':';

        static readonly Method[] Methods = (Method[])Enum.GetValues(typeof(Method));
        static readonly byte[][] MethodsBytes = Enum.GetNames(typeof(Method)).Select(Encoding.ASCII.GetBytes).ToArray();
        static readonly byte[] EndOfPath = { (byte)'?', SP };
        static readonly string[] Versions = { "HTTP/1.1", "HTTP/1.0" };
        static readonly byte[][] VersionsBytes = Versions.Select(Encoding.ASCII.GetBytes).ToArray();

        static readonly byte[] HeaderNameBytesAllowed = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_0123456789");

        internal static bool TryReadLine(byte[] buffer,
                                         ref int start,
                                         int end,
                                         out int lineStart,
                                         out int lineEnd)
        {
            int eolIdx;

            if ((eolIdx = buffer.FindFirstCRLF(start, end)) >= 0)
            {
                lineStart = start;
                lineEnd = eolIdx;
                start = eolIdx + 2;
                return true;
            }

            if ((eolIdx = buffer.FindFirstLF(start, end)) >= 0)
            {
                lineStart = start;
                lineEnd = eolIdx;
                start = eolIdx + 2;
                return true;
            }

            if ((eolIdx = buffer.FindFirstCR(start, end)) >= 0)
            {
                lineStart = start;
                lineEnd = eolIdx;
                start = eolIdx + 3;
                return true;
            }

            lineStart = -1;
            lineEnd = -1;
            return false;
        }

        public static void ParseRequestLine(byte[] buffer,
                                            int lineStart,
                                            int lineEnd,
                                            out Method method,
                                            out string path,
                                            out string query,
                                            out string version)
        {
            int matchedIndex;
            if (!buffer.TryExactMatches(ref lineStart, lineEnd, MethodsBytes, out matchedIndex))
            {
                throw new ParseRequestException("Invalid request line (method)");
            }
            method = Methods[matchedIndex];

            if (!buffer.TryMatchSpaces(ref lineStart, lineEnd))
            {
                throw new ParseRequestException("Invalid request line (after method)");
            }

            if (!buffer.TryMatchUntilAnyOf(ref lineStart, lineEnd, EndOfPath, out path))
            {
                throw new ParseRequestException("Invalid request line (path)");
            }

            if (buffer[lineStart] == (byte)'?')
            {
                if (!buffer.TryMatchUntil(ref lineStart, lineEnd, SP, out query))
                {
                    query = string.Empty;
                }
            }
            else
            {
                query = string.Empty;
            };

            lineStart++;

            if (!buffer.TryExactMatches(ref lineStart, lineEnd, VersionsBytes, out matchedIndex))
            {
                throw new ParseRequestException("Invalid request line (version)");
            }
            version = Versions[matchedIndex];
        }

        public static bool TryParseRequestLine(byte[] buffer,
                                               ref int start,
                                               int end,
                                               out Method method,
                                               out string path,
                                               out string query,
                                               out string version)
        {
            int lineStart, lineEnd;

            if (TryReadLine(buffer, ref start, end, out lineStart, out lineEnd))
            {
                ParseRequestLine(buffer, lineStart, lineEnd, out method, out path, out query, out version);
                return true;
            }
            else
            {
                method = Method.GET;
                path = null;
                query = null;
                version = null;
                return false;
            }
        }

        static string GetLineForDebug(byte[] buffer, int lineStart, int lineEnd, int maxChars)
        {
            return Encoding.ASCII.GetString(buffer, lineStart, Math.Min(maxChars, lineEnd - lineStart - 1));
        }

        //TODO: probably mutate header names to lowercase
        public static void ParseHeader(byte[] buffer,
                                       int lineStart,
                                       int lineEnd,
                                       out string name,
                                       out string value)
        {
            var headerLineStart = lineStart;

            if (!buffer.TryMatchMany(ref lineStart, lineEnd, HeaderNameBytesAllowed, out name))
            {
                throw new ParseRequestException(string.Format("Invalid header name - '{0}'", GetLineForDebug(buffer, headerLineStart, lineEnd, 128)));
            }

            buffer.TryMatchSpaces(ref lineStart, lineEnd);

            if (!buffer.TryMatch(ref lineStart, COLON))
            {
                throw new ParseRequestException(string.Format("Invalid header line (expecting colon) - '{0}'", GetLineForDebug(buffer, headerLineStart, lineEnd, 128)));
            }

            buffer.TryMatchSpaces(ref lineStart, lineEnd);

            value = Encoding.ASCII.GetString(buffer, lineStart, lineEnd - lineStart);
        }

        public static bool TryParseHeaders(byte[] buffer,
                                           ref int start,
                                           int end,
                                           Action<string, string> onHeader)
        {
            int lineStart, lineEnd;
            while (TryReadLine(buffer, ref start, end, out lineStart, out lineEnd))
            {
                if (lineStart == lineEnd)
                {
                    return true;
                }

                string name, value;
                ParseHeader(buffer, lineStart, lineEnd, out name, out value);
                onHeader(name, value);
            }

            return false;
        }

        static HttpRequest ParseHttpRequest(HttpRequest state)
        {
            var host = state.GetHeader("Host");
            var contentType = state.GetHeaderWithDefault(HttpHeader.ContentType, null);
            var connection = state.GetHeaderWithDefault(HttpHeader.Connection, null);
            var isKeepAlive = false;

            var url = new Uri(string.Format("{0}://{1}{2}", state.IsSecureConnection ? "https" : "http", host, state.Path));

            if (connection != null)
            {
                isKeepAlive = connection.IndexOf("keep-alive", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            state.Host = host;
            state.Url = url;
            state.ContentType = contentType;
            state.IsKeepAlive = isKeepAlive;

            return state;
        }

        //TODO: split to TryParseHttpRequest(out hasBody) + TryParseHttpRequestBody (IF we want to read ahead)
        public static bool TryParseHttpRequest(byte[] buffer,
                                               ref int start,
                                               int end,
                                               HttpRequest state,
                                               out HttpRequest parsedRequest)
        {
            while (true)
            {
                switch (state.State)
                {
                    case State.ReadRequestLine:
                        Method method;
                        string path, query, version;
                        if (TryParseRequestLine(buffer, ref start, end, out method, out path, out query, out version))
                        {
                            state.Method = method;
                            state.Path = path;
                            state.Query = query;
                            state.State = State.ReadHeaders;
                            continue;
                        }
                        else
                        {
                            parsedRequest = null;
                            return false;
                        }

                    case State.ReadHeaders:
                        if (TryParseHeaders(buffer, ref start, end, state.SetHeader))
                        {
                            switch (state.Method)
                            {
                                case Method.GET:
                                case Method.HEAD:
                                case Method.DELETE:
                                case Method.OPTIONS:
                                    state.ContentLength = state.GetHeaderWithDefault<int>(HttpHeader.ContentLength, 0);
                                    if (state.ContentLength > 0) //TODO: or 'Transfer-Encoding: chunked'
                                    {
                                        throw new ParseRequestException("Request body not accepted for [GET|HEAD|DELETE|OPTIONS]", HttpStatusCode.BadRequest);
                                    }
                                    state.Body = EmptyRequestStream.Instance;
                                    parsedRequest = ParseHttpRequest(state);
                                    return true;

                                case Method.POST:
                                case Method.PUT:
                                    // 1) Fully-readahead path
                                    state.ContentLength = state.GetHeaderWithDefault<int>(HttpHeader.ContentLength, -1);
                                    if (state.ContentLength < 0)
                                    {
                                        throw new ParseRequestException("Content-Length required", HttpStatusCode.LengthRequired);
                                    }
                                    state.Body = new MemoryRequestStream(state.ContentLength); //TODO: guard length + support 'Transfer-Encoding: chunked'
                                    state.State = State.ReadBodyToEnd;
                                    // 2) TODO: pass (wrapped) networkstream through to client for reading
                                    continue;

                                default:
                                    throw new ParseRequestException(state.Method + " not supported", HttpStatusCode.MethodNotAllowed); //TODO
                            }
                        }
                        else
                        {
                            parsedRequest = null;
                            return false;
                        }

                    case State.ReadBodyToEnd:
                        var requestBody = (MemoryRequestStream)state.Body;
                        var bytesAvailable = end - start; // may span multiple requests (eg. pipelined)
                        var bytesOutstanding = state.ContentLength - (int)requestBody.Length;
                        var bytesToWrite = Math.Min(bytesAvailable, bytesOutstanding); // only consume enough for the current request
                        requestBody.WriteRequestBody(buffer, start, bytesToWrite);
                        start += bytesToWrite;
                        if (requestBody.Length == state.ContentLength)
                        {
                            requestBody.ResetPosition();
                            parsedRequest = ParseHttpRequest(state);
                            state.State = State.Completed;
                            return true;
                        }
                        else
                        {
                            parsedRequest = null;
                            return false;
                        }
                }
            }
        }
    }
}
