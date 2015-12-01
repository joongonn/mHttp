using System;
using System.Threading;
using System.Threading.Tasks;

namespace m.DB
{
    class Thing
    {
        public readonly int Id;

        #region Test instrumentation flags
        internal bool IsBrokenFlag = false;
        #endregion
        
        public Thing(int id)
        {
            Id = id;
        }
    }

    class MockPool : LazyPool<Thing>
    {
        internal int maxThingId = 0;

        #region Test instrumentation flags
        internal TimeSpan AcquireDelay = TimeSpan.Zero;
        #endregion

        public MockPool(string name, int maxPoolSize, TimeSpan defaultPoolTimeout) : base(name, maxPoolSize, defaultPoolTimeout) { }

        protected override Task<Thing> AcquireNewResourceAsync()
        {
            Task.Delay(AcquireDelay);
            var nextThing = new Thing(Interlocked.Increment(ref maxThingId));

            return Task.FromResult(nextThing);
        }

        protected override bool IsResourceBroken(Thing resource)
        {
            return resource.IsBrokenFlag;
        }
    }
}

