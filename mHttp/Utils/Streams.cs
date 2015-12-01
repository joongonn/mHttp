using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace m.Utils
{
    public static class Streams
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

        public static void WriteUTF8(this Stream stream, string content)
        {
            stream.Write(content, Encoding.UTF8);
        }

        public static void WriteAscii(this Stream stream, string content)
        {
            stream.Write(content, Encoding.ASCII);
        }

        public static void Write(this Stream stream, string content, Encoding encoding)
        {
            var bytes = encoding.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
