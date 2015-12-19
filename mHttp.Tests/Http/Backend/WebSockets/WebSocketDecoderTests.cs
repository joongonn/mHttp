using System;
using System.Text;

using NUnit.Framework;

namespace m.Http.Backend.WebSockets
{
    [TestFixture]
    public class WebSocketDecoderTests : BaseTest
    {
        [Test]
        public void TestTryDecodeUnmaskedText()
        {
            var frame = new byte[] { 0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f }; // "Hello"

            int start = 0;
            bool isFin, isMasked;
            OpCode opCode;
            var mask = new byte[4];
            int payloadLength;

            Assert.IsTrue(FrameDecoder.TryDecodeHeader(frame, ref start, frame.Length, out isFin, out opCode, out isMasked, out payloadLength, mask));
            Assert.IsTrue(isFin);
            Assert.AreEqual(OpCode.Text, opCode);
            Assert.IsFalse(isMasked);
            Assert.AreEqual(5, payloadLength);

            var decodePayloadStart = start;
            Assert.IsTrue(FrameDecoder.TryDecodePayload(frame, ref start, frame.Length, payloadLength, isMasked, mask));
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(frame, decodePayloadStart, payloadLength));
        }

        [Test]
        public void TestTryDecodeMaskedText()
        {
            int start = 0;
            bool isFin, isMasked;
            OpCode opCode;
            var mask = new byte[4];
            int payloadLength;

            var frame = new byte[] { 0x81, 0x85, 0x37, 0xfa, 0x21, 0x3d, 0x7f, 0x9f, 0x4d, 0x51, 0x58 }; // "Hello"

            Assert.IsTrue(FrameDecoder.TryDecodeHeader(frame, ref start, frame.Length, out isFin, out opCode, out isMasked, out payloadLength, mask));
            Assert.IsTrue(isFin);
            Assert.AreEqual(OpCode.Text, opCode);
            Assert.IsTrue(isMasked);
            Assert.AreEqual(5, payloadLength);

            var decodePayloadStart = start;
            Assert.IsTrue(FrameDecoder.TryDecodePayload(frame, ref start, frame.Length, payloadLength, isMasked, mask));
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(frame, decodePayloadStart, payloadLength));
        }

        [Test]
        public void TestTryDecodeFragmentedUnmaskedText()
        {
            int start = 0;
            bool isFin, isMasked;
            OpCode opCode;
            var mask = new byte[4];
            int payloadLength;

            var frame0 = new byte[] { 0x01, 0x03, 0x48, 0x65, 0x6c }; // "Hel"

            Assert.IsTrue(FrameDecoder.TryDecodeHeader(frame0, ref start, frame0.Length, out isFin, out opCode, out isMasked, out payloadLength, mask));
            Assert.IsFalse(isFin);
            Assert.AreEqual(OpCode.Text, opCode);
            Assert.IsFalse(isMasked);
            Assert.AreEqual(3, payloadLength);

            var decodePayloadStart = start;
            Assert.IsTrue(FrameDecoder.TryDecodePayload(frame0, ref start, frame0.Length, payloadLength, isMasked, mask));
            Assert.AreEqual("Hel", Encoding.UTF8.GetString(frame0, decodePayloadStart, payloadLength));

            var frame1 = new byte[] { 0x80, 0x02, 0x6c, 0x6f }; // "lo"

            start = 0;
            Assert.IsTrue(FrameDecoder.TryDecodeHeader(frame1, ref start, frame1.Length, out isFin, out opCode, out isMasked, out payloadLength, mask));
            Assert.IsTrue(isFin);
            Assert.AreEqual(OpCode.Continuation, opCode);
            Assert.IsFalse(isMasked);
            Assert.AreEqual(2, payloadLength);

            decodePayloadStart = start;
            Assert.IsTrue(FrameDecoder.TryDecodePayload(frame1, ref start, frame1.Length, payloadLength, isMasked, mask));
            Assert.AreEqual("lo", Encoding.UTF8.GetString(frame1, decodePayloadStart, payloadLength));
        }
    }
}
