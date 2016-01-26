using System;
using System.IO;

namespace m.Http.Backend
{
    abstract class RequestStream : Stream
    {
        public override bool CanRead { get; } = true;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = false;

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    class EmptyRequestStream : RequestStream
    {
        public static readonly EmptyRequestStream Instance = new EmptyRequestStream();

        public override long Length { get; } = 0;

        public override int Read(byte[] buffer, int offset, int count) => 0;

        EmptyRequestStream() { }

        protected override void Dispose(bool disposing) { }
    }

    class MemoryRequestStream : RequestStream
    {
        readonly MemoryStream requestBody;

        public override long Length { get { return requestBody.Length; } }

        public MemoryRequestStream(int contentLength)
        {
            requestBody = new MemoryStream(contentLength);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return requestBody.Read(buffer, offset, count);
        }

        internal void WriteRequestBody(byte[] buffer, int offset, int count)
        {
            requestBody.Write(buffer, offset, count);
        }

        internal void ResetPosition()
        {
            requestBody.Position = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                requestBody.Dispose();
            }
        }
    }
}
