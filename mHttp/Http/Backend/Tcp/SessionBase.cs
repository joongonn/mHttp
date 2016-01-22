using System;
using System.IO;
using System.Threading.Tasks;

using m.Utils;

namespace m.Http.Backend.Tcp
{
    abstract class SessionBase
    {
        public long Id { get; private set; }
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
            if (dataStart == readBufferOffset)
            {
                dataStart = 0;
                readBufferOffset = 0;
            }
            else
            {
                int available = readBufferOffset - dataStart;
                for (int i=0; i<available; i++)
                {
                    readBuffer[i] = readBuffer[dataStart + i];
                }

                dataStart = 0;
                readBufferOffset = available;
            }
        }

        internal async Task<int> ReadToBufferAsync()
        {
            var bufferRemaining = readBuffer.Length - readBufferOffset;
            if (bufferRemaining == 0)
            {
                BufferUtils.Expand(ref readBuffer, readBuffer.Length);
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
