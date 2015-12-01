using System;
using System.Threading.Tasks;

using NUnit.Framework;

namespace m.DB
{
    [TestFixture]
    public class LazyPoolSingleThreadTests : BaseTest
    {
        MockPool pool;

        [SetUp]
        public void Setup()
        {
            pool = new MockPool("mock", 2, TimeSpan.FromSeconds(2));
        }

        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public async void TestGetTimeout()
        {
            var p1 = await pool.GetAsync();
            var p2 = await pool.GetAsync();

            Assert.AreEqual(1, p1.Resource.Id);
            Assert.AreEqual(2, p2.Resource.Id);
            Assert.AreEqual(2, pool.maxThingId);

            await pool.GetAsync(); // Timesout
        }

        [Test]
        public async void TestDispose()
        {
            var get1 = pool.GetAsync();
            var get2 = pool.GetAsync();
            var get3 = pool.GetAsync();

            await Task.Delay(TimeSpan.FromSeconds(1));

            var p1 = await get1;
            var p2 = await get2;

            Assert.AreEqual(0, pool.CurrentFreeCount);
            Assert.AreEqual(2, pool.CurrentPoolSize);

            p1.Dispose();
            p2.Dispose();

            Assert.AreEqual(2, pool.CurrentFreeCount);
            Assert.AreEqual(2, pool.CurrentPoolSize);

            var p3 = await get3;
            Assert.LessOrEqual(p3.Resource.Id, 2);
            Assert.AreEqual(pool.CurrentPoolSize, 2);
        }

        [Test]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async void TestDisposeMultipleTimes()
        {
            var p1 = await pool.GetAsync();
            Assert.AreEqual(1, p1.Resource.Id);

            p1.Dispose();

            Assert.DoesNotThrow(p1.Dispose);
            Assert.DoesNotThrow(p1.Dispose);

            Assert.AreEqual(1, p1.Resource.Id);; // exception
        }

        [Test]
        public async void TestReturnBrokenResource()
        {
            var p1 = await pool.GetAsync();
            var p2 = await pool.GetAsync();

            Assert.AreEqual(0, pool.CurrentFreeCount);
            Assert.AreEqual(2, pool.CurrentPoolSize);

            p1.Resource.IsBrokenFlag = true;
            p1.Dispose();
            p2.Dispose();

            Assert.AreEqual(1, pool.CurrentFreeCount);
            Assert.AreEqual(1, pool.CurrentPoolSize);

            var p3 = await pool.GetAsync();
            var p4 = await pool.GetAsync();

            Assert.AreEqual(0, pool.CurrentFreeCount);
            Assert.AreEqual(2, pool.CurrentPoolSize);
            Assert.AreEqual(3, pool.maxThingId);

            p3.Resource.IsBrokenFlag = true;
            p4.Resource.IsBrokenFlag = true;
            p3.Dispose();
            p4.Dispose();

            Assert.AreEqual(0, pool.CurrentFreeCount);
            Assert.AreEqual(0, pool.CurrentPoolSize);
        }
    }
}
