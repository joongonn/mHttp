using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace m.Http.Routing
{
    [TestFixture]
    public class RouteTests : BaseTest
    {
        [Test]
        public void TestRouteOrder()
        {
            var routes = new []
            {
                new Route("/"),
                new Route("/1"),
                new Route("/1/2/{a}"),
                new Route("/1/{a}"),
                new Route("/1/*"),
                new Route("/1/2/3"),
                new Route("/1/{a}/{b}"),
                new Route("/{a}/*"),
                new Route("/*"),
                new Route("/{a}"),
                new Route("/1/2/*"),
                new Route("/1/{a}/3"),
                new Route("/{a}/{b}/{c}")
            };

            Array.Sort(routes);

            Assert.AreEqual("/1/2/3", routes[0].PathTemplate);
            Assert.AreEqual("/1/2/{a}", routes[1].PathTemplate);
            Assert.AreEqual("/1/2/*", routes[2].PathTemplate);
            Assert.AreEqual("/1/{a}/3", routes[3].PathTemplate);
            Assert.AreEqual("/1/{a}/{b}", routes[4].PathTemplate);
            Assert.AreEqual("/{a}/{b}/{c}", routes[5].PathTemplate);
            Assert.AreEqual("/1/{a}", routes[6].PathTemplate);
            Assert.AreEqual("/1/*", routes[7].PathTemplate);
            Assert.AreEqual("/{a}/*", routes[8].PathTemplate);
            Assert.AreEqual("/1", routes[9].PathTemplate);
            Assert.AreEqual("/{a}", routes[10].PathTemplate);
            Assert.AreEqual("/*", routes[11].PathTemplate);
            Assert.AreEqual("/", routes[12].PathTemplate);
        }

        [Test]
        public void TestRouteMatching()
        {
            IReadOnlyDictionary<string, string> pathVariables;

            var r0 = new Route("/");
            Assert.True(r0.TryMatch(new Uri("http://localhost/"), out pathVariables));
            Assert.True(r0.TryMatch(new Uri("http://localhost"), out pathVariables));
            Assert.True(r0.TryMatch(new Uri("http://localhost?k=v"), out pathVariables));

            var r1 = new Route("/*");
            Assert.True(r1.TryMatch(new Uri("http://localhost/"), out pathVariables));
            Assert.True(r1.TryMatch(new Uri("http://localhost/whatever"), out pathVariables));
            Assert.True(r1.TryMatch(new Uri("http://localhost?k=v"), out pathVariables));

            var r2 = new Route("/files/*");
            Assert.False(r2.TryMatch(new Uri("http://localhost/files"), out pathVariables));
            Assert.True(r2.TryMatch(new Uri("http://localhost/files/"), out pathVariables));

            var r3 = new Route("/accounts");
            Assert.True(r3.TryMatch(new Uri("http://localhost/accounts"), out pathVariables));
            Assert.AreEqual(0, pathVariables.Count);
            Assert.False(r3.TryMatch(new Uri("http://localhost/accounts/"), out pathVariables));
            Assert.IsNull(pathVariables);

            var r4 = new Route("/accounts/{id}");
            Assert.True(r4.TryMatch(new Uri("http://localhost/accounts/1234"), out pathVariables));
            Assert.False(r4.TryMatch(new Uri("http://localhost/accounts/1234/data"), out pathVariables));

            var r5 = new Route("/accounts/{id}/data");
            Assert.True(r5.TryMatch(new Uri("http://localhost/accounts/1234/data"), out pathVariables));
            Assert.False(r5.TryMatch(new Uri("http://localhost/accounts/1234/data/"), out pathVariables));
            Assert.True(r5.TryMatch(new Uri("http://localhost/accounts/1234/data?keys=name"), out pathVariables));
            Assert.AreEqual("1234", pathVariables["id"]);
            Assert.False(r5.TryMatch(new Uri("http://localhost/accounts/1234"), out pathVariables));

            var r6 = new Route("/images/{category}/c/{name}");
            Assert.True(r6.TryMatch(new Uri("http://localhost/images/animals/c/cat.png"), out pathVariables));
            Assert.AreEqual(2, pathVariables.Count);
            Assert.AreEqual("animals", pathVariables["category"]);
            Assert.AreEqual("cat.png", pathVariables["name"]);
            Assert.False(r6.TryMatch(new Uri("http://localhost/images/animals/c"), out pathVariables));

            var r7 = new Route("/files/*");
            Assert.True(r7.TryMatch(new Uri("http://localhost/files/test/test.png"), out pathVariables));
            Assert.True(r7.TryMatch(new Uri("http://localhost/files/test.png"), out pathVariables));
            Assert.False(r7.TryMatch(new Uri("http://localhost/files"), out pathVariables));
        }
    }
}
