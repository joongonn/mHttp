using System.IO;
using System.IO.Compression;

namespace m.Utils
{
    public static class Compression
    {
        public static byte[] GZip(byte[] bytes)
        {
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    gz.Write(bytes, 0, bytes.Length);
                }

                return ms.ToArray();
            }
        }
    }
}
