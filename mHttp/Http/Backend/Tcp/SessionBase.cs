using System;
using System.IO;
using System.Threading.Tasks;

using m.Utils;

namespace m.Http.Backend.Tcp
{
    abstract class SessionBase
    {
        public long Id { get; }
        protected Stream inputStream;

        protected byte[] readBuffer;
        protected int readBufferOffset = 0;

        protected SessionBase(long id, Stream inputStream, int initialReadBufferSize)
        {
            Id = id;
            this.inputStream = inputStream;
            readBuffer = new byte[initialReadBufferSize];
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
                var bytesRead = await inputStream.ReadAsync(readBuffer, readBufferOffset, bufferRemaining).ConfigureAwait(false);

                readBufferOffset += bytesRead;
                return bytesRead;
            }
            catch (Exception e)
            {
                throw new SessionStreamException("Exception while reading from stream", e);
            }
        }
    }
}
