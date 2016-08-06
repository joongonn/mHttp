using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using m.Http.Backend.WebSockets;
using m.Utils;

using m.Http;

namespace m.Http.Backend.Tcp
{
    //TODO: guards eg. payloadLength spoofing dos, bounded buffers
    class WebSocketSession : TcpSessionBase, IWebSocketSession
    {
        enum DecodeState
        {
            DecodeHeader,
            DecodePayload
        }

        readonly object stateLock = new object();
        readonly Action<int> onBytesReceived;
        readonly Action<int> onBytesSent;
        readonly Action onDisposed;

        int dataStart;

        #region Frame states
        DecodeState decodeState;
        OpCode frameOpCode;
        bool isFin, isMasked;
        int framePayloadLength;
        byte[] mask;
        #endregion

        #region Message states (only for [Text|Binary])
        OpCode messageOpCode;
        byte[] messageBuffer;
        int messageBufferOffset;
        #endregion

        volatile bool sentClose = false;
        volatile bool closed = false;

        public bool IsOpen { get { return !closed; } }

        public WebSocketSession(long id,
                                TcpClient tcpClient,
                                Stream stream,
                                Action<int> onBytesReceived,
                                Action<int> onBytesSent,
                                Action onDisposed,
                                int initialReadBufferSize,
                                int readTimeoutMs,
                                int writeTimeoutMs) : base(id, tcpClient, stream, initialReadBufferSize, readTimeoutMs, writeTimeoutMs)
        {
            this.onBytesReceived = onBytesReceived;
            this.onBytesSent = onBytesSent;
            this.onDisposed = onDisposed;
            decodeState = DecodeState.DecodeHeader;
            dataStart = 0;
            mask = new byte[4];
        }

        bool TryDecodeNextMessage(out OpCode opCode, out byte[] messagePayload)
        {
            while (true)
            {
                switch (decodeState)
                {
                    case DecodeState.DecodeHeader:
                        if (FrameDecoder.TryDecodeHeader(readBuffer, ref dataStart, readBufferOffset, out isFin, out frameOpCode, out isMasked, out framePayloadLength, mask))
                        {
                            decodeState = DecodeState.DecodePayload;

                            switch (frameOpCode)
                            {
                                case OpCode.Continuation:
                                    if (framePayloadLength > 0)
                                    {
                                        BufferUtils.Expand(ref messageBuffer, framePayloadLength); //TODO: limit, sanity guard
                                    }
                                    break;

                                case OpCode.Text:
                                case OpCode.Binary:
                                    messageOpCode = frameOpCode;
                                    messageBuffer = new byte[framePayloadLength];
                                    messageBufferOffset = 0;
                                    break;

                                case OpCode.Close:
                                case OpCode.Ping:
                                case OpCode.Pong:
                                    if (!isFin)
                                    {
                                        throw new WebSocketException("Control frame cannot be fragmented");
                                    }
                                    break;
                            }
                            
                            continue;
                        }
                        break;
    
                    case DecodeState.DecodePayload:
                        var decodeStart = dataStart;
                        if (FrameDecoder.TryDecodePayload(readBuffer, ref dataStart, readBufferOffset, framePayloadLength, isMasked, mask))
                        {
                            decodeState = DecodeState.DecodeHeader;

                            switch (frameOpCode)
                            {
                                case OpCode.Continuation:
                                case OpCode.Text:
                                case OpCode.Binary:
                                    Array.Copy(readBuffer, decodeStart, messageBuffer, messageBufferOffset, framePayloadLength);
                                    CompactReadBuffer(ref dataStart);
                                    if (isFin)
                                    {
                                        opCode = messageOpCode;
                                        messagePayload = messageBuffer;
                                        messageBuffer = null;
                                        messageBufferOffset = 0;
                                        return true;
                                    }
                                    else
                                    {
                                        messageBufferOffset += framePayloadLength;
                                        continue;
                                    }

                                case OpCode.Close:
                                case OpCode.Ping:
                                case OpCode.Pong:
                                    opCode = frameOpCode;
                                    messagePayload = new byte[framePayloadLength];
                                    Array.Copy(readBuffer, decodeStart, messagePayload, 0, framePayloadLength);
                                    CompactReadBuffer(ref dataStart);
                                    return true; // must be Fin
                            }
                        }
                        break;
                }

                opCode = OpCode.Continuation;
                messagePayload = null;
                return false;
            }
        }

        public async Task<WebSocketMessage> ReadNextMessageAsync()
        {
            while (true)
            {
                OpCode opCode;
                byte[] payload;

                while (TryDecodeNextMessage(out opCode, out payload))
                {
                    if (closed)
                    {
                        throw new WebSocketException(string.Format("Session already closed (dropping received message:[{0}] payloadLength:[{1}])", opCode, payload.Length));
                    }

                    switch (opCode)
                    {
                        case OpCode.Text:
                            return new WebSocketMessage.Text(Encoding.UTF8.GetString(payload));

                        case OpCode.Binary:
                            return new WebSocketMessage.Binary(payload);

                        case OpCode.Close:
                            ushort statusCode;
                            string reason;
                            FrameDecoder.TryDecodeCloseReason(payload, out statusCode, out reason);
                            return new WebSocketMessage.Close(statusCode, reason);

                        case OpCode.Ping:
                            return new WebSocketMessage.Ping();

                        case OpCode.Pong:
                            return new WebSocketMessage.Pong();
                    }
                }

                int bytesRead;
                try
                {
                    bytesRead = await ReadToBufferAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new WebSocketException("Exception reading from websocket stream (server closed?)", e);
                }

                if (bytesRead > 0)
                {
                    onBytesReceived(bytesRead);
                    continue;
                }
                else
                {
                    throw new WebSocketException("Disconnected (client)");
                }
            }
        }

        public void SendText(string text)
        {
            if (closed)
            {
                throw new WebSocketException("Session already closed");
            }

            Write(OpCode.Text, Encoding.UTF8.GetBytes(text));
        }

        public void SendBinary(byte[] blob)
        {
            if (closed)
            {
                throw new WebSocketException("Session already closed");
            }

            Write(OpCode.Binary, blob);
        }

        public void SendClose(ushort statusCode=0, string reason=null)
        {
            lock (stateLock)
            {
                if (sentClose)
                {
                    return;
                }

                sentClose = true;
            }

            Write(OpCode.Close, FrameEncoder.GetClosePayload(statusCode, reason));
        }

        public void SendPing()
        {
            if (closed)
            {
                throw new WebSocketException("Session already closed");
            }

            Write(OpCode.Ping);
        }

        public void SendPong()
        {
            if (closed)
            {
                throw new WebSocketException("Session already closed");
            }

            Write(OpCode.Pong);
        }

        void Write(OpCode opCode, byte[] payload=null)
        {
            var writeBufferSize = 4 + (payload == null ? 0 : payload.Length);

            using (var ms = new MemoryStream(writeBufferSize))
            {
                int bytesWritten = FrameEncoder.Write(ms, opCode, false, payload);

                try
                {
                    lock (stateLock)
                    {
                        Write(ms.GetBuffer(), 0, bytesWritten);
                        onBytesSent(bytesWritten);
                    }
                }
                catch (SessionStreamException e)
                {
                    throw new WebSocketException("Exception writing to websocket stream", e);
                }
            }
        }

        public void CloseSession(ushort statusCode=0, string reason=null)
        {
            lock (stateLock)
            {
                if (closed)
                {
                    return;
                };

                closed = true;
            }

            try
            {
                SendClose(statusCode, reason);
            }
            catch
            {
                return;
            }
            finally
            {
                CloseQuiety();
            }
        }

        public void Dispose()
        {
            try
            {
                CloseSession();
            }
            finally
            {
                onDisposed();
            }
        }
    }
}
