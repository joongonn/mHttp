using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using NUnit.Framework;

namespace m.Http
{
    [TestFixture]
    public class RouterTests : BaseTest
    {
        static readonly IReadOnlyDictionary<string, string> EmptyHeaders = new Dictionary<string, string>()
        {
            { "Host", "localhost" }
        };

        [Test]
        public async void TestRouter()
        {
            var routeTable = new RouteTable(
                Route.Get("/").With(() => new TextResponse("root")),
                Route.Get("/1").With(() => new TextResponse("one")),
                Route.Get("/2").With(() => new TextResponse("two")),
                Route.Get("/3").With(() => new TextResponse("three"))
            );

            var router = new Router(routeTable);

            var req1 = new HttpRequest(Method.GET, ContentTypes.Plain, EmptyHeaders, new Uri("http://localhost/invalid"), false, new MemoryStream());
            var resp1 = await router.HandleHttpRequest(req1, DateTime.UtcNow);
            Assert.AreEqual(HttpStatusCode.NotFound, resp1.StatusCode);

            var req2 = new HttpRequest(Method.GET, ContentTypes.Plain, EmptyHeaders, new Uri("http://localhost/1"), false, new MemoryStream());
            var resp2 = await router.HandleHttpRequest(req2, DateTime.UtcNow);
            Assert.IsInstanceOf<TextResponse>(resp2);
            Assert.AreEqual(ContentTypes.Plain, resp2.ContentType);
            Assert.AreEqual("one", System.Text.Encoding.UTF8.GetString(resp2.Body));
        }
    }
}

