using System;
using System.Net;

namespace m.Http
{
    public enum Method
    {
        GET,
        HEAD,
        POST,
        PUT,
        DELETE,
        CONNECT,
        OPTIONS,
        TRACE
    }

    static class MethodExtensions
    {
        public static Method GetMethod(string s)
        {
            switch (s)
            {
                case "GET"     : return Method.GET;
                case "HEAD"    : return Method.HEAD;
                case "POST"    : return Method.POST;
                case "PUT"     : return Method.PUT;
                case "DELETE"  : return Method.DELETE;
                case "CONNECT" : return Method.CONNECT;
                case "OPTIONS" : return Method.OPTIONS;
                case "TRACE"   : return Method.TRACE;

                default:
                    throw new NotSupportedException(string.Format("{0} method not supported", s));
            }
        }

        public static Method GetMethod(this HttpListenerRequest req)
        {
            return GetMethod(req.HttpMethod);
        }
    }
}
