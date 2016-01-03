using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace m.Utils
{
    static class Streams
    {
        public static string ReadToEnd(this Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static Task<string> ReadToEndAsync(this Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEndAsync();
            }
        }

        public static int Write(this Stream stream, byte[] content)
        {
            stream.Write(content, 0, content.Length);
            return content.Length;
        }

        public static int WriteUTF8(this Stream stream, string content)
        {
            return stream.Write(content, Encoding.UTF8);
        }

        public static int WriteAscii(this Stream stream, string content)
        {
            return stream.Write(content, Encoding.ASCII);
        }

        public static int WriteAsciiFormat(this Stream stream, string content, params object[] objects)
        {
            return stream.Write(string.Format(content, objects), Encoding.ASCII);
        }

        public static int Write(this Stream stream, string content, Encoding encoding)
        {
            var bytes = encoding.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);

            return bytes.Length;
        }
    }
}
