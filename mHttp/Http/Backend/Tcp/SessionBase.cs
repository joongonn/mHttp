using System;
using System.IO;
using System.Threading.Tasks;

using m.Utils;

namespace m.Http.Backend.Tcp
{
    abstract class SessionBase
    {
        public long Id { get; }
        internal Stream Stream { get; }

        protected byte[] readBuffer;
        protected int readBufferOffset;

        protected SessionBase(long id, Stream stream, int initialReadBufferSize)
        {
            Id = id;
            Stream = stream;
            readBuffer = new byte[initialReadBufferSize];
            readBufferOffset = 0;
        }

        protected void CompactReadBuffer(ref int dataStart)
        {
            readBuffer.Compact(ref dataStart, ref readBufferOffset);
        }

        internal async Task<int> ReadToBufferAsync()
        {
            var bufferRemaining = readBuffer.Length - readBufferOffset;
            if (bufferRemaining == 0)
            {
                BufferUtils.Expand(ref readBuffer, readBuffer.Length); // double buffer
                bufferRemaining = readBuffer.Length - readBufferOffset;
            }

            try
            {
                var bytesRead = await Stream.ReadAsync(readBuffer, readBufferOffset, bufferRemaining).ConfigureAwait(false);

                readBufferOffset += bytesRead;
                return bytesRead;
            }
            catch (Exception e)
            {
                throw new SessionStreamException("Exception while reading from stream", e);
            }
        }

        internal void Write(byte[] buffer, int offset, int size)
        {
            try
            {
                Stream.Write(buffer, offset, size);
            }
            catch (Exception e)
            {
                throw new SessionStreamException("Exception writing to stream", e);
            }
        }
    }
}
