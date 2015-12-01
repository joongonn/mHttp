using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace m.Http.Backend.Tcp
{
    class RequestState
    {
        public enum State
        {
            ReadRequestLine,
            ReadHeaders,
            ReadBody
        }

        public State CurrentState { get; set; }
        public Method Method { get; set; }
        public string Path { get ; set; }
        public Dictionary<string, string> Headers { get; }

        public int ContentLength { get; set; }
        public MemoryStream Body { get; set; }

        public RequestState()
        {
            Headers = new Dictionary<string, string>(16, StringComparer.OrdinalIgnoreCase);
        }

        public void SetHeader(string name, string value)
        {
            Headers[name] = value;
        }

        public string GetHeader(string nameIgnoreCase)
        {
            string value;
            if (Headers.TryGetValue(nameIgnoreCase, out value))
            {
                return value;
            }
            else
            {
                throw new RequestException(string.Format("'{0}' header not found", nameIgnoreCase), HttpStatusCode.BadRequest);
            }
        }

        public string GetHeaderWithDefault(string nameIgnoreCase, string defaultValue)
        {
            string value;
            if (Headers.TryGetValue(nameIgnoreCase, out value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }


        public T GetHeader<T>(string name)
        {
            var value = GetHeader(name);
            var converter = TypeDescriptor.GetConverter(typeof(T));

            return (T)converter.ConvertFromString(value);
        }
    }
}