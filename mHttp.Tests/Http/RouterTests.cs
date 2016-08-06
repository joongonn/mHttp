using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using NUnit.Framework;

using m.Http.Backend;

namespace m.Http
{
    [TestFixture]
    public class RouterTests : BaseTest
    {
        static readonly IPEndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);
        
        [Test]
        public async void TestRouter()
        {
            var routeTable = new RouteTable(
                Route.Get("/").With(() => new TextResponse("root")),
                Route.Get("/1").With(() => new TextResponse("one")),
                Route.Get("/2").With(() => new TextResponse("two")),
                Route.Get("/3").With(() => new TextResponse("three")),
                Route.Get("/{number}/capture").With(() => new TextResponse("some number"))
            );

            var router = new Router(routeTable);

            var req1 = new HttpRequest(RemoteEndPoint, false, "localhost", Method.GET, new Uri("http://localhost/invalid"), "/invalid", string.Empty, new Dictionary<string, string>(), ContentTypes.Plain, 0, false, new MemoryStream());
            var result1 = await router.HandleRequest(req1, DateTime.UtcNow);
            var resp1 = result1.HttpResponse;
            Assert.AreEqual(HttpStatusCode.NotFound, resp1.StatusCode);

            var req2 = new HttpRequest(RemoteEndPoint, false, "localhost", Method.GET, new Uri("http://localhost/1"), "/1", string.Empty, new Dictionary<string, string>(), ContentTypes.Plain, 0, false, new MemoryStream());
            var result2 = await router.HandleRequest(req2, DateTime.UtcNow);
            var resp2 = result2.HttpResponse;
            Assert.IsInstanceOf<TextResponse>(resp2);
            Assert.AreEqual(ContentTypes.Plain, resp2.ContentType);
            using (var ms = new MemoryStream())
            {
                var bytesWritten = await resp2.Body.WriteToAsync(ms);
                Assert.AreEqual("one".Length, bytesWritten);
                Assert.AreEqual("one", System.Text.Encoding.UTF8.GetString(ms.GetBuffer(), 0, bytesWritten));
            }

            var req3 = new HttpRequest(RemoteEndPoint, false, "localhost", Method.GET, new Uri("http://localhost/8/capture"), "/8/capture", string.Empty, new Dictionary<string, string>(), ContentTypes.Plain, 0, false, new MemoryStream());
            await router.HandleRequest(req3, DateTime.UtcNow);
            Assert.AreEqual("8", req3.PathVariables["number"]);
        }
    }
}

