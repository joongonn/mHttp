using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace m.Http.Backend.Tcp
{
    abstract class SessionBase
    {
        readonly Stream inputStream;

        protected byte[] buffer;
        protected int bufferOffset = 0;

        protected SessionBase(Stream inputStream, int initialReadBufferSize)
        {
            this.inputStream = inputStream;
            buffer = new byte[initialReadBufferSize];
        }

        void ResizeBuffer()
        {
            var newBuffer = new byte[buffer.Length * 2];
            Array.Copy(buffer, newBuffer, buffer.Length);
            buffer = newBuffer;
        }

        public async Task<int> ReadToBufferAsync()
        {
            var bufferRemaining = buffer.Length - bufferOffset;
            if (bufferRemaining == 0)
            {
                ResizeBuffer();
                bufferRemaining = buffer.Length - bufferOffset;
            }

            try
            {
                var bytesRead = await inputStream.ReadAsync(buffer, bufferOffset, bufferRemaining).ConfigureAwait(false);

                bufferOffset += bytesRead;
                return bytesRead;
            }
            catch (Exception e)
            {
                throw new SessionStreamException("Exception while reading from stream", e);
            }
        }
    }
}

