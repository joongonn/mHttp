using System;
using System.Text;

namespace m.Http.Backend.WebSockets
{
    static class FrameDecoder
    {
        static OpCode GetOpCode(int b)
        {
            var opCode = (OpCode)b;

            switch (opCode)
            {
                case OpCode.Continuation:
                case OpCode.Text:
                case OpCode.Binary:
                case OpCode.Close:
                case OpCode.Ping:
                case OpCode.Pong:
                    return opCode;
                
                default:
                    throw new WebSocketException(string.Format("Unrecognized opCode:[{0}]", opCode));
            }
        }

        public static bool TryDecodeHeader(byte[] buffer,
                                           ref int dataStart,
                                           int end,
                                           out bool isFin,
                                           out OpCode opCode,
                                           out bool isMasked,
                                           out int payloadLength,
                                           byte[] mask)
        {
            if (end - dataStart >= 2)
            {
                var byte0 = buffer[dataStart];
                var byte1 = buffer[dataStart + 1];

                isFin = (byte0 & 128) == 128;
                opCode = GetOpCode(byte0 & 15);
                isMasked = (byte1 & 128) == 128; //TODO: exception if not masked and client
                long len = byte1 & 127;

                if (len <= 125)
                {
                    var bytesRequired = isMasked ? (2 + 4) : 2;

                    if (end - dataStart >= bytesRequired)
                    {
                        if (isMasked)
                        {
                            mask[0] = buffer[dataStart + 2];
                            mask[1] = buffer[dataStart + 3];
                            mask[2] = buffer[dataStart + 4];
                            mask[3] = buffer[dataStart + 5];
                        }

                        dataStart += bytesRequired;
                        payloadLength = (int)len;
                        return true;
                    }
                }

                if (len == 126)
                {
                    var bytesRequired = isMasked ? (4 + 4) : 4;

                    if (end - dataStart >= bytesRequired)
                    {
                        len =
                            (long)buffer[dataStart + 2] << 8 |
                            (long)buffer[dataStart + 3];

                        if (isMasked)
                        {
                            mask[0] = buffer[dataStart + 4];
                            mask[1] = buffer[dataStart + 5];
                            mask[2] = buffer[dataStart + 6];
                            mask[3] = buffer[dataStart + 7];
                        }

                        dataStart += bytesRequired;
                        payloadLength = (int)len;
                        return true;
                    }
                }

                if (len == 127)
                {
                    var bytesRequired = isMasked ? (10 + 4) : 10;

                    if (end - dataStart >= bytesRequired)
                    {
                        if ((buffer[dataStart + 2] & (byte)128) == 0)
                        {
                            len =
                                (long)buffer[dataStart + 2] << 56 |
                                (long)buffer[dataStart + 3] << 48 |
                                (long)buffer[dataStart + 4] << 40 |
                                (long)buffer[dataStart + 5] << 32 |
                                (long)buffer[dataStart + 6] << 24 |
                                (long)buffer[dataStart + 7] << 16 |
                                (long)buffer[dataStart + 8] << 8 |
                                (long)buffer[dataStart + 9];

                            if (isMasked)
                            {
                                mask[0] = buffer[dataStart + 10];
                                mask[1] = buffer[dataStart + 11];
                                mask[2] = buffer[dataStart + 12];
                                mask[3] = buffer[dataStart + 13];
                            }

                            dataStart += bytesRequired;
                            payloadLength = (int)len;
                            return true;
                        }
                        else
                        {
                            throw new WebSocketException("MSB of payload length not zero");
                        }
                    }
                }
            }

            isFin = false;
            opCode = OpCode.Continuation;
            isMasked = false;
            payloadLength = -1;
            return false;
        }

        public static bool TryDecodePayload(byte[] buffer,
                                            ref int start,
                                            int end,
                                            int payloadLength,
                                            bool isMasked,
                                            byte[] mask)
        {
            if (end - start >= payloadLength)
            {
                if (isMasked)
                {
                    for (int i=0; i<payloadLength; i++)
                    {
                        buffer[start + i] = (byte)(buffer[start + i] ^ mask[i % 4]);
                    }
                }

                start += payloadLength;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool TryDecodeCloseReason(byte[] closePayload, out ushort statusCode, out string reason)
        {
            int payloadLength = closePayload.Length;

            if (payloadLength >= 2)
            {
                statusCode = (ushort)((closePayload[0] << 8) | closePayload[1]);

                if (payloadLength > 2)
                {
                    reason = Encoding.UTF8.GetString(closePayload, 2, payloadLength - 2);
                }
                else
                {
                    reason = null;
                }

                return true;
            }
            else
            {
                statusCode = 0;
                reason = null;
                return false;
            }
        }
    }
}
