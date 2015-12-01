﻿using System;
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

            IEndpoint ep1 = Route.Get("/").WithAction(noOp);
            IEndpoint ep2 = Route.Get("/accounts/{id}/data").WithAction(noOp);
            IEndpoint ep3 = Route.Get("/files/*").WithAction(noOp);
            IEndpoint ep4 = Route.Get("/accounts/{id}").WithAction(noOp);
            IEndpoint ep5 = Route.Post("/accounts/").WithAction(noOp);

            var routeTable = new RouteTable(ep1, ep2, ep3, ep4, ep5);

            IReadOnlyDictionary<string, string> urlVariables;

            Assert.AreEqual(0, routeTable.TryMatchEndpoint(Method.GET, new Uri("http://localhost/"), out urlVariables));
            Assert.AreSame(routeTable[0], ep1);

            Assert.AreEqual(1, routeTable.TryMatchEndpoint(Method.GET, new Uri("http://localhost/accounts/111/data?keys=name"), out urlVariables));
            Assert.AreSame(routeTable[1], ep2);
            Assert.AreEqual("111", urlVariables["id"]);
            Assert.AreEqual(-1, routeTable.TryMatchEndpoint(Method.POST, new Uri("http://localhost/accounts/111/data"), out urlVariables));
            Assert.IsNull(urlVariables);

            Assert.AreEqual(2, routeTable.TryMatchEndpoint(Method.GET, new Uri("http://localhost/files/images/test.png"), out urlVariables));
            Assert.AreSame(routeTable[2], ep3);

            Assert.AreEqual(3, routeTable.TryMatchEndpoint(Method.GET, new Uri("http://localhost/accounts/222"), out urlVariables));
            Assert.AreSame(routeTable[3], ep4);
            Assert.AreEqual("222", urlVariables["id"]);

            Assert.AreEqual(-1, routeTable.TryMatchEndpoint(Method.POST, new Uri("http://localhost/"), out urlVariables));
        }
    }
}
