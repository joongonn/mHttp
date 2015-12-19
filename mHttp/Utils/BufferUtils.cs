using System;

namespace m.Utils
{
    public static class BufferUtils
    {
        public static void Expand(ref byte[] buffer, int space)
        {
            var newBuffer = new byte[buffer.Length + space];
            Array.Copy(buffer, newBuffer, buffer.Length);
            buffer = newBuffer;
        }
    }
}
