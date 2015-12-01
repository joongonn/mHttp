using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace m.Http.Routing
{
    [TestFixture]
    public class RouteTests : BaseTest
    {
        [Test]
        public void TestRoute()
        {
            IReadOnlyDictionary<string, string> urlVariables;

            var r1 = new Route("/");
            Assert.True(r1.TryMatch(new Uri("http://localhost/"), out urlVariables));
            Assert.True(r1.TryMatch(new Uri("http://localhost"), out urlVariables));
            Assert.True(r1.TryMatch(new Uri("http://localhost?k=v"), out urlVariables));

            var r2 = new Route("/accounts");
            Assert.True(r2.TryMatch(new Uri("http://localhost/accounts"), out urlVariables));
            Assert.AreEqual(0, urlVariables.Count);
            Assert.True(r2.TryMatch(new Uri("http://localhost/accounts/"), out urlVariables));
            Assert.False(r2.TryMatch(new Uri("http://localhost/account"), out urlVariables));
            Assert.IsNull(urlVariables);

            var r3 = new Route("/accounts/{id}");
            Assert.True(r3.TryMatch(new Uri("http://localhost/accounts/1234"), out urlVariables));
            Assert.False(r3.TryMatch(new Uri("http://localhost/accounts/1234/data"), out urlVariables));

            var r4 = new Route("/accounts/{id}/data");
            Assert.True(r4.TryMatch(new Uri("http://localhost/accounts/1234/data"), out urlVariables));
            Assert.True(r4.TryMatch(new Uri("http://localhost/accounts/1234/data/"), out urlVariables));
            Assert.True(r4.TryMatch(new Uri("http://localhost/accounts/1234/data?keys=name"), out urlVariables));
            Assert.AreEqual("1234", urlVariables["id"]);
            Assert.False(r4.TryMatch(new Uri("http://localhost/accounts/1234"), out urlVariables));

            var r5 = new Route("/images/{category}/c/{name}");
            Assert.True(r5.TryMatch(new Uri("http://localhost/images/animals/c/cat.png"), out urlVariables));
            Assert.AreEqual(2, urlVariables.Count);
            Assert.AreEqual("animals", urlVariables["category"]);
            Assert.AreEqual("cat.png", urlVariables["name"]);
            Assert.False(r5.TryMatch(new Uri("http://localhost/images/animals/c"), out urlVariables));

            var r6 = new Route("/files/*");
            Assert.True(r6.TryMatch(new Uri("http://localhost/files/test/test.png"), out urlVariables));
            Assert.True(r6.TryMatch(new Uri("http://localhost/files/test.png"), out urlVariables));
            Assert.False(r6.TryMatch(new Uri("http://localhost/files"), out urlVariables));
        }
    }
}
