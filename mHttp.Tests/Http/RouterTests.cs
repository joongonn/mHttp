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
        static readonly IReadOnlyDictionary<string, string> EmptyHeaders = new Dictionary<string, string>();

        [Test]
        public async void TestRouter()
        {
            var routeTable = new RouteTable(
                Route.Get("/").With(() => HttpResponse.Text("root")),
                Route.Get("/1").With(() => HttpResponse.Text("one")),
                Route.Get("/2").With(() => HttpResponse.Text("two")),
                Route.Get("/3").With(() => HttpResponse.Text("three"))
            );

            var router = new Router(routeTable);

            var req1 = new HttpRequest(Method.GET, ContentTypes.Plain, EmptyHeaders, new Uri("http://localhost/invalid"), false, new MemoryStream());
            var resp1 = await router.HandleHttpRequest(req1, DateTime.UtcNow);
            Assert.AreEqual(HttpStatusCode.NotFound, resp1.StatusCode);

            var req2 = new HttpRequest(Method.GET, ContentTypes.Plain, EmptyHeaders, new Uri("http://localhost/1"), false, new MemoryStream());
            var resp2 = await router.HandleHttpRequest(req2, DateTime.UtcNow);
            Assert.IsInstanceOf<HttpResponse.TextResponse>(resp2);
            Assert.AreEqual(ContentTypes.Plain, resp2.ContentType);
            Assert.AreEqual("one", System.Text.Encoding.UTF8.GetString(resp2.Body));
        }
    }
}

