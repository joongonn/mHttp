using System;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace m.DB
{
    [TestFixture]
    public class LazyPoolMultiThreadTests : BaseTest
    {
        const int N = 32;
        const int PoolSize = 8;

        static LazyPoolMultiThreadTests()
        {
            ThreadPool.SetMinThreads((int)(N * 1.5), 1);
            ThreadPool.SetMaxThreads((int)(N * 1.5), 1);
        }

        MockPool pool;

        [SetUp]
        public void Setup()
        {
            pool = new MockPool("mock", PoolSize, TimeSpan.FromSeconds(2));
        }

        [Test]
        public void TestPoolContention()
        {
            var barrier = new Barrier(N);
            var tasks = new Task[N];
            var totalTimesAcquired = 0;

            Assert.AreEqual(pool.maxThingId, 0);

            for (int i = 0; i < N; i++)
            {
                var t = Task.Run(() => {
                    barrier.SignalAndWait(); // Takes time for all threads to spin up
                    for (int j = 0; j < 128; j++)
                    {
                        using (var pooled = pool.GetAsync().Result) // blocks
                        {
                            Interlocked.Increment(ref totalTimesAcquired);
                        }
                    }
                });

                tasks[i] = t; // These N tasks do not yield their thread
            }

            Task.WaitAll(tasks);

            Assert.AreEqual(PoolSize, pool.maxThingId);
            Assert.AreEqual(PoolSize, pool.CurrentFreeCount);
            Assert.AreEqual(PoolSize, pool.CurrentPoolSize);
            Assert.AreEqual(N * 128, totalTimesAcquired);
        }
   }
}
