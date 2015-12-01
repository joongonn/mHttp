using System;
using System.Linq;
using System.Text;
using System.IO;

namespace m.Http.Backend.Tcp
{
    static class RequestParser
    {
        const byte CR = 13;
        const byte LF = 10;
        const byte SP = 32;
        const byte COLON = (byte)':';

        static readonly Method[] Methods = (Method[])Enum.GetValues(typeof(Method));
        static readonly byte[][] MethodsBytes = Enum.GetNames(typeof(Method)).Select(m => Encoding.ASCII.GetBytes(m)).ToArray();
        static readonly string[] Versions = new string[] { "HTTP/1.1", "HTTP/1.0" };
        static readonly byte[][] VersionsBytes = Versions.Select(v => Encoding.ASCII.GetBytes(v)).ToArray();

        static readonly byte[] HeaderNameBytesAllowed = Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_");

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

            if (!TryMatchUntil(buffer, ref lineStart, lineEnd, SP, out path))
            {
                throw new ParseRequestException("Invalid request line (path)");
            }

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
                                               out string version)
        {
            int lineStart, lineEnd;
            if (TryReadLine(buffer, ref start, end, out lineStart, out lineEnd))
            {
                ParseRequestLine(buffer, lineStart, lineEnd, out method, out path, out version);
                return true;
            }
            else
            {
                method = Method.GET;
                path = null;
                version = null;
                return false;
            }
        }

        public static void ParseHeader(byte[] buffer,
                                       int lineStart,
                                       int lineEnd,
                                       out string name,
                                       out string value)
        {
            if (!TryMatchMany(buffer, ref lineStart, lineEnd, HeaderNameBytesAllowed, out name))
            {
                throw new ParseRequestException("Invalid header line (name)");
            }

            TryMatchSpaces(buffer, ref lineStart, lineEnd);

            if (!TryMatch(buffer, ref lineStart, lineEnd, COLON))
            {
                throw new ParseRequestException("Invalid header line (expecting colon)");
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

        public static HttpRequest ParseHttpRequest(RequestState state)
        {
            var host = state.GetHeader("Host");
            var connection = state.GetHeaderWithDefault("Connection", null);
            var contentType = state.GetHeaderWithDefault("Content-Type", null);

            state.Body.Position = 0;
            var url = new Uri(string.Format("{0}://{1}{2}", "http", host, state.Path));
            var isKeepAlive = string.Equals("Keep-Alive", connection, StringComparison.OrdinalIgnoreCase);

            return new HttpRequest(state.Method,
                                   contentType,
                                   state.Headers,
                                   url,
                                   isKeepAlive,
                                   state.Body);
        }

        public static bool TryParseHttpRequest(byte[] buffer,
                                               ref int start,
                                               int end,
                                               RequestState state,
                                               out HttpRequest httpRequest)
        {
            while (true)
            {
                switch (state.CurrentState)
                {
                    case RequestState.State.ReadRequestLine:
                        Method method;
                        string path, version;
                        if (TryParseRequestLine(buffer, ref start, end, out method, out path, out version))
                        {
                            state.Method = method;
                            state.Path = path;
                            state.CurrentState = RequestState.State.ReadHeaders;
                            continue;
                        }
                        else
                        {
                            httpRequest = null;
                            return false;
                        }

                    case RequestState.State.ReadHeaders:
                        if (TryParseHeaders(buffer, ref start, end, state.SetHeader))
                        {
                            switch (state.Method)
                            {
                                case Method.GET:
                                case Method.DELETE:
                                    state.ContentLength = 0;
                                    state.Body = new MemoryStream(0);
                                    httpRequest = ParseHttpRequest(state);
                                    return true;

                                case Method.POST:
                                case Method.PUT:
                                    //TODO:Transfer-Encoding: chunked
                                    state.ContentLength = state.GetHeader<int>(Headers.ContentLength);
                                    state.Body = new MemoryStream(state.ContentLength);
                                    state.CurrentState = RequestState.State.ReadBody;
                                    continue;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                        {
                            httpRequest = null;
                            return false;
                        }

                case RequestState.State.ReadBody:
                        int bytesAvailable = end - start;
                        int bytesOutstanding = state.ContentLength - (int)state.Body.Length;
                        int bytesToWrite = Math.Min(bytesAvailable, bytesOutstanding);
                        state.Body.Write(buffer, start, bytesToWrite);
                        start += bytesToWrite;
                        if (state.Body.Length == state.ContentLength)
                        {
                            httpRequest = ParseHttpRequest(state);
                            return true;
                        }
                        else
                        {
                            httpRequest = null;
                            return false;
                        }
                        
                }
            }
        }
    }
}
