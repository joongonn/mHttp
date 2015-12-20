using System;
using System.IO;

using Newtonsoft.Json;

namespace m.Http
{
    public static class Json
    {
        public static string ToJson(this object o)
        {
            return JsonConvert.SerializeObject(o);
        }

        public static string ToPrettyJson(this object o)
        {
            return JsonConvert.SerializeObject(o, Formatting.Indented);
        }

        public static object FromJson(this string s, Type toType)
        {
            return JsonConvert.DeserializeObject(s, toType);
        }

        public static T FromJson<T>(this string s)
        {
            return JsonConvert.DeserializeObject<T>(s);
        }

        public static T FromJson<T>(this Stream inputstream)
        {
            return FromJson<T>(inputstream.ReadToEnd());
        }

        static string ReadToEnd(this Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
