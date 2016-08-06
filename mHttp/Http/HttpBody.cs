using System;
using System.IO;
using System.Threading.Tasks;

using m.Http.Backend.Tcp;

namespace m.Http
{
    //TODO: Chunked responses
    public abstract class HttpBody
    {
        public abstract long Length { get; } //TODO: unknown length (chunked response)

        HttpBody() { }

        internal abstract Task<int> WriteToAsync(Stream toStream);

        public sealed class Empty : HttpBody
        {
            public static readonly Empty Instance = new Empty();

            readonly Task<int> Zero = Task.FromResult(0);

            public override long Length { get ; } = 0;

            Empty() { }

            internal override Task<int> WriteToAsync(Stream toStream) => Zero;
        }

        public sealed class ByteArray : HttpBody
        {
            public byte[] Bytes { get; }
            public override long Length { get { return Bytes.Length; } }

            public ByteArray(byte[] bytes)
            {
                Bytes = bytes;
            }

            internal override async Task<int> WriteToAsync(Stream toStream)
            {
                try
                {
                    var len = (int)Length;
                    await toStream.WriteAsync(Bytes, 0, len).ConfigureAwait(false); //TODO: handle int cast assumption
                    return len;
                }
                catch (Exception e)
                {
                    throw new SessionStreamException("Exception writing to stream", e);
                }
            }
        }

        public sealed class Streamable : HttpBody, IDisposable
        {
            readonly Stream source;
            readonly int blockSize;

            public override long Length { get; } //TODO: unknown length

            public Streamable(Stream source, long length, int blockSize=4096)
            {
                this.source = source;
                Length = length;
                this.blockSize = blockSize;
            }

            internal override async Task<int> WriteToAsync(Stream toStream)
            {
                var buffer = new byte[blockSize];
                int bytesRead, totalBytesRead = 0;

                //TODO: try BufferedStream
                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    totalBytesRead += bytesRead;

                    try
                    {
                        await toStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        throw new SessionStreamException("Exception writing to stream", e);
                    }
                }

                return totalBytesRead;
            }

            public void Dispose()
            {
                source.Dispose();
            }
        }
    }
}
