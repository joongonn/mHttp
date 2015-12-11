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

            var req1 = new HttpRequest("localhost", Method.GET, new Uri("http://localhost/invalid"), "/invalid", string.Empty, new Dictionary<string, string>(), ContentTypes.Plain, 0, false, new MemoryStream());
            var resp1 = await router.HandleRequest(req1, DateTime.UtcNow);
            Assert.AreEqual(HttpStatusCode.NotFound, resp1.StatusCode);

            var req2 = new HttpRequest("localhost", Method.GET, new Uri("http://localhost/1"), "/1", string.Empty, new Dictionary<string, string>(), ContentTypes.Plain, 0, false, new MemoryStream());
            var resp2 = await router.HandleRequest(req2, DateTime.UtcNow);
            Assert.IsInstanceOf<TextResponse>(resp2);
            Assert.AreEqual(ContentTypes.Plain, resp2.ContentType);
            Assert.AreEqual("one", System.Text.Encoding.UTF8.GetString(resp2.Body));

            var req3 = new HttpRequest("localhost", Method.GET, new Uri("http://localhost/8/capture"), "/8/capture", string.Empty, new Dictionary<string, string>(), ContentTypes.Plain, 0, false, new MemoryStream());
            await router.HandleRequest(req3, DateTime.UtcNow);
            Assert.AreEqual("8", req3.PathVariables["number"]);

        }
    }
}

