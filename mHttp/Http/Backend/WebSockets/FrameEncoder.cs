using System;
using System.IO;
using System.Text;

namespace m.Http.Backend.WebSockets
{
    static class FrameEncoder
    {
        public static byte[] GetClosePayload(uint statusCode, string reason)
        {
            byte[] closePayload = null;

            if (statusCode > 0)
            {
                if (!string.IsNullOrEmpty(reason))
                {
                    var reasonBytes = Encoding.UTF8.GetBytes(reason);
                    closePayload = new byte[2 + reasonBytes.Length];
                    Array.Copy(reasonBytes, 0, closePayload, 2, reasonBytes.Length);
                }
                else
                {
                    closePayload = new byte[2];
                }

                closePayload[0] = (byte)((statusCode & 0xFF00) >> 8);
                closePayload[1] = (byte)(statusCode & 0x00FF);
            }

            return closePayload ?? new byte[0];
        }

        public static int Write(Stream stream, OpCode opCode, bool masked=false, byte[] payload=null, int maxFrameSize=128*1024)
        {
            var payloadLength = (payload == null) ? 0 : payload.Length;

            if ((opCode == OpCode.Close || opCode == OpCode.Ping || opCode == OpCode.Pong) && payloadLength > 125)
            {
                throw new ArgumentOutOfRangeException("payload", "Control frame payload cannot exceed 125 bytes");
            }

            int framesToWrite;
            var bytesWritten = 0;

            if (payloadLength > 0)
            {
                framesToWrite = (payloadLength / maxFrameSize);
                if (payloadLength % maxFrameSize > 0)
                {
                    framesToWrite++;
                }
            }
            else
            {
                framesToWrite = 1;
            }

            for (int i=0; i<framesToWrite; i++)
            {
                var isFin = i == (framesToWrite - 1);
                var thisFrameOpCode = (i == 0) ? opCode : OpCode.Continuation;
                var thisFrameLength = isFin ? (payloadLength - i * maxFrameSize) : maxFrameSize;

                // 1. Write OpCode
                stream.WriteByte((byte)((isFin ? 128 : 0) | (byte)thisFrameOpCode));
                bytesWritten += 1;

                // 2. Write Length
                if (thisFrameLength <= 125)
                {
                    stream.WriteByte((byte)((masked ? 128 : 0) | thisFrameLength));
                    bytesWritten += 1;
                }
                else if (thisFrameLength <= 65535)
                {
                    var len = thisFrameLength & 0xFFFF;
                    stream.WriteByte((byte)126);
                    stream.WriteByte((byte)((len & 0xFF00) >> 8));
                    stream.WriteByte((byte)(len & 0xFF));
                    bytesWritten += 1 + 2;
                }
                else
                {
                    var len = (ulong)thisFrameLength;
                    stream.WriteByte((byte)127);
                    stream.WriteByte((byte)((len & 0xFF00000000000000L) >> 54));
                    stream.WriteByte((byte)((len & 0x00FF000000000000L) >> 48));
                    stream.WriteByte((byte)((len & 0x0000FF0000000000L) >> 40));
                    stream.WriteByte((byte)((len & 0x000000FF00000000L) >> 32));
                    stream.WriteByte((byte)((len & 0x00000000FF000000L) >> 24));
                    stream.WriteByte((byte)((len & 0x0000000000FF0000L) >> 16));
                    stream.WriteByte((byte)((len & 0x000000000000FF00L) >> 8));
                    stream.WriteByte((byte)((len & 0x00000000000000FFL)));
                    bytesWritten += 1 + 8;
                }

                // 3. Write Payload
                if (thisFrameLength > 0)
                {
                    if (masked)
                    {
                        throw new NotImplementedException(); //TODO
                    }
                    else
                    {
                        stream.Write(payload, i * maxFrameSize, thisFrameLength);
                        bytesWritten += thisFrameLength;
                    }
                }
            }

            return bytesWritten;
        }
    }
}
