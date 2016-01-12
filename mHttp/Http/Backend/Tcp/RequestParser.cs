using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace m.Http.Backend.Tcp
{
    static class RequestParser
    {
        public enum State
        {
            ReadRequestLine,
            ReadHeaders,
            ReadBody,
            Completed
        }

        const byte CR = 13;
        const byte LF = 10;
        const byte SP = 32;
        const byte COLON = (byte)':';

        static readonly Method[] Methods = (Method[])Enum.GetValues(typeof(Method));
        static readonly byte[][] MethodsBytes = Enum.GetNames(typeof(Method)).Select(m => Encoding.ASCII.GetBytes(m)).ToArray();
        static readonly byte[] EndOfPath = new byte[] { (byte)'?', SP };
        static readonly string[] Versions = new string[] { "HTTP/1.1", "HTTP/1.0" };
        static readonly byte[][] VersionsBytes = Versions.Select(v => Encoding.ASCII.GetBytes(v)).ToArray();

        static readonly byte[] HeaderNameBytesAllowed = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_0123456789");

        static int FindFirstCRLF(byte[] buffer, int start, int end)
        {
            for (int i=start; i<end-1; i++)
            {
                if (buffer[i] == CR && buffer[i + 1] == LF)
                {
                    return i;
                }
            }

            return -1;
        }

        static int FindFirstCR(byte[] buffer, int start, int end)
        {
            for (int i=start; i<end; i++)
            {
                if (buffer[i] == CR)
                {
                    return i;
                }
            }

            return -1;
        }

        static int FindFirstLF(byte[] buffer, int start, int end)
        {
            for (int i=start; i<end; i++)
            {
                if (buffer[i] == LF)
                {
                    return i;
                }
            }

            return -1;
        }

        static bool TryExactMatch(byte[] buffer,
                                  ref int start,
                                  int end,
                                  byte[] sequence)
        {
            if (start + sequence.Length > end)
            {
                return false;
            }

            for (int i=0; i<sequence.Length; i++)
            {
                if (buffer[start + i] != sequence[i])
                {
                    return false;
                }
            }

            start = start + sequence.Length;
            return true;
        }

        static bool TryExactMatches(byte[] buffer,
                                    ref int start,
                                    int end,
                                    byte[][] sequences,
                                    out int matchedIndex)
        {
            for (int i=0; i<sequences.Length; i++)
            {
                if (TryExactMatch(buffer, ref start, end, sequences[i]))
                {
                    matchedIndex = i;
                    return true;
                }
            }

            matchedIndex = -1;
            return false;
        }

        static bool TryMatchSpaces(byte[] buffer, ref int start, int end)
        {
            var initial = start;

            while (start < end && buffer[start] == SP)
            {
                start++;
            }

            return start > initial;
        }

        static bool TryMatchUntil(byte[] buffer,
                                  ref int start,
                                  int end,
                                  byte value,
                                  out string matched)
        {
            int i = start;

            while (i < end)
            {
                if (buffer[i] == value)
                {
                    matched = Encoding.ASCII.GetString(buffer, start, i - start);
                    start = i;
                    return true;
                }
                i++;
            }

            matched = null;
            return false;
        }

        static bool TryMatchUntilAnyOf(byte[] buffer,
                                       ref int start,
                                       int end,
                                       byte[] values,
                                       out string matched)
        {
            int i = start;

            while (i < end)
            {
                if (values.Contains(buffer[i]))
                {
                    matched = Encoding.ASCII.GetString(buffer, start, i - start);
                    start = i;
                    return true;
                }
                i++;
            }

            matched = null;
            return false;
        }

        static bool TryMatch(byte[] buffer,
                             ref int start,
                             int end,
                             byte value)
        {
            if (buffer[start] == value)
            {
                start++;
                return true;
            }
            else
            {
                return false;
            }
        }

        static bool Contains(this byte[] values, byte v)
        {
            for (int i=0; i<values.Length; i++)
            {
                if (values[i] == v)
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryMatchMany(byte[] buffer,
                                 ref int start,
                                 int end,
                                 byte[] values,
                                 out string matched)
        {
            int i = start;
            while (i < end && values.Contains(buffer[i]))
            {
                i++;
            }

            if (i > start)
            {
                matched = Encoding.ASCII.GetString(buffer, start, i-start);
                start = i;
                return true;
            }
            else
            {
                matched = null;
                return false;
            }
        }

        public static bool TryReadLine(byte[] buffer,
                                       ref int start,
                                       int end,
                                       out int lineStart,
                                       out int lineEnd)
        {
            int eolIdx;

            if ((eolIdx = FindFirstCRLF(buffer, start, end)) >= 0)
            {
                lineStart = start;
                lineEnd = eolIdx;
                start = eolIdx + 2;
                return true;
            }

            if ((eolIdx = FindFirstLF(buffer, start, end)) >= 0)
            {
                lineStart = start;
                lineEnd = eolIdx;
                start = eolIdx + 2;
                return true;
            }

            if ((eolIdx = FindFirstCR(buffer, start, end)) >= 0)
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
            if (!TryExactMatches(buffer, ref lineStart, lineEnd, MethodsBytes, out matchedIndex))
            {
                throw new ParseRequestException("Invalid request line (method)");
            }
            method = Methods[matchedIndex];

            if (!TryMatchSpaces(buffer, ref lineStart, lineEnd))
            {
                throw new ParseRequestException("Invalid request line (after method)");
            }

            if (!TryMatchUntilAnyOf(buffer, ref lineStart, lineEnd, EndOfPath, out path))
            {
                throw new ParseRequestException("Invalid request line (path)");
            }

            if (buffer[lineStart] == (byte)'?')
            {
                if (!TryMatchUntil(buffer, ref lineStart, lineEnd, SP, out query))
                {
                    query = string.Empty;
                }
            }
            else
            {
                query = string.Empty;
            };

            lineStart++;

            if (!TryExactMatches(buffer, ref lineStart, lineEnd, VersionsBytes, out matchedIndex))
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

        public static void ParseHeader(byte[] buffer,
                                       int lineStart,
                                       int lineEnd,
                                       out string name,
                                       out string value)
        {
            var headerLineStart = lineStart;

            if (!TryMatchMany(buffer, ref lineStart, lineEnd, HeaderNameBytesAllowed, out name))
            {
                throw new ParseRequestException(string.Format("Invalid header name - '{0}'", GetLineForDebug(buffer, headerLineStart, lineEnd, 128)));
            }

            TryMatchSpaces(buffer, ref lineStart, lineEnd);

            if (!TryMatch(buffer, ref lineStart, lineEnd, COLON))
            {
                throw new ParseRequestException(string.Format("Invalid header line (expecting colon) - '{0}'", GetLineForDebug(buffer, headerLineStart, lineEnd, 128)));
            }

            TryMatchSpaces(buffer, ref lineStart, lineEnd);

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

            state.Body.Position = 0;

            state.Host = host;
            state.Url = url;
            state.ContentType = contentType;
            state.IsKeepAlive = isKeepAlive;

            return state;
        }

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
                                    state.ContentLength = 0;
                                    state.Body = new MemoryStream(0);
                                    parsedRequest = ParseHttpRequest(state);
                                    return true;

                                case Method.POST:
                                case Method.PUT:
                                    //FIXME:Transfer-Encoding: chunked
                                    var contentLength = state.GetHeader<int>(HttpHeader.ContentLength);
                                    state.ContentLength = contentLength;
                                    state.Body = new MemoryStream(contentLength);
                                    state.State = State.ReadBody;
                                    continue;

                                default:
                                    throw new RequestException(state.Method + " not supported", HttpStatusCode.MethodNotAllowed); //TODO
                            }
                        }
                        else
                        {
                            parsedRequest = null;
                            return false;
                        }

                    case State.ReadBody:
                        int bytesAvailable = end - start;
                        int bytesOutstanding = state.ContentLength - (int)state.Body.Length;
                        int bytesToWrite = Math.Min(bytesAvailable, bytesOutstanding);
                        state.Body.Write(buffer, start, bytesToWrite);
                        start += bytesToWrite;
                        if (state.Body.Length == state.ContentLength)
                        {
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
