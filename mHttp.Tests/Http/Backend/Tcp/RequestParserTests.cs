using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

using NUnit.Framework;

using m.Http.Backend.Tcp;

namespace m.Http.Backend.Tcp
{
    [TestFixture]
    public class RequestParserTests : BaseTest
    {
        static readonly IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);

        static int WriteAscii(Stream stream, string content)
        {
            var bytes = Encoding.ASCII.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }

        [Test]
        [Ignore("TODO: guard rogue client")]
        public void TestMixedNewLines()
        {
            var request = new MemoryStream(8192);
            var buffer = request.GetBuffer();
            var start = 0;

            int lineStart, lineEnd;

            WriteAscii(request, "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:42.0) Gecko/20100101 Firefox/42.0\n");
            WriteAscii(request, "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\n");
            WriteAscii(request, "Accept-language: en-US;q=0.7,en;q=0.3\n");
            WriteAscii(request, "Connection: close\r\n");

            Assert.IsTrue(RequestParser.TryReadLine(buffer, ref start, (int)request.Length, out lineStart, out lineEnd));
            var line = Encoding.ASCII.GetString(buffer, lineStart, lineEnd);
            Assert.AreEqual("User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:42.0) Gecko/20100101 Firefox/42.0", line);
        }

        [Test]
        public void TestTryReadLine()
        {
            var request = new MemoryStream(8192);
            var buffer = request.GetBuffer();
            var start = 0;

            int lineStart, lineEnd;

            Assert.IsFalse(RequestParser.TryReadLine(buffer, ref start, (int)request.Length, out lineStart, out lineEnd));

            WriteAscii(request, "\r\n");
            Assert.IsTrue(RequestParser.TryReadLine(buffer, ref start, (int)request.Length, out lineStart, out lineEnd));

            WriteAscii(request, "GET /");
            Assert.IsFalse(RequestParser.TryReadLine(buffer, ref start, (int)request.Length, out lineStart, out lineEnd));

            WriteAscii(request, " HTTP/1.1\r\nUser-Agent: ");
            Assert.IsTrue(RequestParser.TryReadLine(buffer, ref start, (int)request.Length, out lineStart, out lineEnd));

            Assert.AreEqual(Encoding.ASCII.GetString(buffer, lineStart, lineEnd - lineStart), "GET / HTTP/1.1");
        }

        [Test]
        public void TestParseRequestLine1()
        {
            var request = new MemoryStream(8192);
            var buffer = request.GetBuffer();
            var start = 0;

            int lineStart, lineEnd;

            WriteAscii(request, "POST /accounts?flag=x HTTP/1.1\r\n");
            Assert.IsTrue(RequestParser.TryReadLine(buffer, ref start, (int)request.Length, out lineStart, out lineEnd));

            Method method;
            string path, query, version;

            RequestParser.ParseRequestLine(buffer, lineStart, lineEnd, out method, out path, out query, out version);
            Assert.AreEqual(Method.POST, method);
            Assert.AreEqual("/accounts", path);
            Assert.AreEqual("?flag=x", query);
            Assert.AreEqual("HTTP/1.1", version);
        }

        [Test]
        public void TestParseRequestLine2()
        {
            var request = new MemoryStream(8192);
            var buffer = request.GetBuffer();
            var start = 0;

            int lineStart, lineEnd;

            WriteAscii(request, "POST /accounts? HTTP/1.1\r\n");
            Assert.IsTrue(RequestParser.TryReadLine(buffer, ref start, (int)request.Length, out lineStart, out lineEnd));

            Method method;
            string path, query, version;

            RequestParser.ParseRequestLine(buffer, lineStart, lineEnd, out method, out path, out query, out version);
            Assert.AreEqual(Method.POST, method);
            Assert.AreEqual("/accounts", path);
            Assert.AreEqual("?", query);
            Assert.AreEqual("HTTP/1.1", version);
        }

        [Test]
        public void TestParseHeader()
        {
            var request = new MemoryStream(8192);
            var buffer = request.GetBuffer();
            var start = 0;

            int lineStart, lineEnd;

            WriteAscii(request, "Host : localhost:8080\r\n");
            Assert.IsTrue(RequestParser.TryReadLine(buffer, ref start, (int)request.Length, out lineStart, out lineEnd));

            string name, value;
            RequestParser.ParseHeader(buffer, lineStart, lineEnd, out name, out value);
            Assert.AreEqual("Host", name);
            Assert.AreEqual("localhost:8080", value);
        }

        [Test]
        public void TestParseHeaders()
        {
            var request = new MemoryStream(8192);
            var buffer = request.GetBuffer();
            var start = 0;

            WriteAscii(request, "User-Agent: curl/7.35.0\r\n");
            WriteAscii(request, "Host: localhost:8080\r\n");
            WriteAscii(request, "Accept: */*\r\n");
            WriteAscii(request, "\r\n");

            var headers = new Dictionary<string, string>();
            Assert.IsTrue(RequestParser.TryParseHeaders(buffer, ref start, (int)request.Length, headers.Add));
        }

        [Test]
        public void TestTryParseHttpRequest()
        {
            var request = new MemoryStream(8192);
            var buffer = request.GetBuffer();
            var start = 0;

            WriteAscii(request, "GET /index.jsp HTTP/1.1\r\n");
            WriteAscii(request, "User-Agent: curl/7.35.0\r\n");
            WriteAscii(request, "Host: localhost:8080\r\n");
            WriteAscii(request, "Accept: */*\r\n");

            var state = new HttpRequest(RemoteEndPoint, false);
            HttpRequest httpRequest;
            Assert.IsFalse(RequestParser.TryParseHttpRequest(buffer, ref start, (int)request.Length, state, out httpRequest));

            WriteAscii(request, "\r\n");
            Assert.IsTrue(RequestParser.TryParseHttpRequest(buffer, ref start, (int)request.Length, state, out httpRequest));
            Assert.AreEqual(Method.GET, ((HttpRequest)httpRequest).Method);
            Assert.AreEqual("http://localhost:8080/index.jsp", httpRequest.Url.AbsoluteUri);
            Assert.AreEqual("curl/7.35.0", httpRequest.Headers["User-Agent"]);
        }
    }
}

