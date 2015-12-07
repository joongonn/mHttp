using System;
using System.Collections.Generic;

using NUnit.Framework;

using m.Http;
using m.Http.Routing;

namespace m.Http
{
    [TestFixture]
    public class RouteTableTests : BaseTest
    {
        [Test]
        public void TestRouteTable()
        {
            Action noOp = () => {};

            Endpoint ep1 = Route.Get("/").WithAction(noOp);
            Endpoint ep2 = Route.Get("/accounts/{id}/data").WithAction(noOp);
            Endpoint ep3 = Route.Get("/files/*").WithAction(noOp);
            Endpoint ep4 = Route.Get("/accounts/{id}").WithAction(noOp);
            Endpoint ep5 = Route.Post("/accounts/").WithAction(noOp);

            var routeTable = new RouteTable(ep1, ep2, ep3, ep4, ep5);

            IReadOnlyDictionary<string, string> pathVariables;

            Assert.AreEqual(0, routeTable.TryMatchEndpoint(Method.GET, new Uri("http://localhost/"), out pathVariables));
            Assert.AreSame(routeTable[0], ep1);

            Assert.AreEqual(1, routeTable.TryMatchEndpoint(Method.GET, new Uri("http://localhost/accounts/111/data?keys=name"), out pathVariables));
            Assert.AreSame(routeTable[1], ep2);
            Assert.AreEqual("111", pathVariables["id"]);
            Assert.AreEqual(-1, routeTable.TryMatchEndpoint(Method.POST, new Uri("http://localhost/accounts/111/data"), out pathVariables));
            Assert.IsNull(pathVariables);

            Assert.AreEqual(2, routeTable.TryMatchEndpoint(Method.GET, new Uri("http://localhost/files/images/test.png"), out pathVariables));
            Assert.AreSame(routeTable[2], ep3);

            Assert.AreEqual(3, routeTable.TryMatchEndpoint(Method.GET, new Uri("http://localhost/accounts/222"), out pathVariables));
            Assert.AreSame(routeTable[3], ep4);
            Assert.AreEqual("222", pathVariables["id"]);

            Assert.AreEqual(-1, routeTable.TryMatchEndpoint(Method.POST, new Uri("http://localhost/"), out pathVariables));
        }
    }
}
