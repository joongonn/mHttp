using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using m.Logging;

namespace m.DB
{
    public abstract class LazyPool<TResource>
    {
        readonly LoggingProvider.ILogger logger;

        public readonly string Label;
        public readonly int MaxPoolSize;
        public readonly TimeSpan PoolTimeout;

        readonly SemaphoreSlim pool;
        readonly ConcurrentBag<TResource> bag;
        int currentPoolSize;
        int currentFreeCount;

        public int CurrentPoolSize { get { return Thread.VolatileRead(ref currentPoolSize); } }
        public int CurrentFreeCount { get { return Thread.VolatileRead(ref currentFreeCount); } }

        protected LazyPool(string label, int maxPoolSize, TimeSpan poolTimeout)
        {
            logger = LoggingProvider.GetLogger(GetType());

            Label = label;
            MaxPoolSize = maxPoolSize;
            PoolTimeout = poolTimeout;
            currentPoolSize = 0;
            currentFreeCount = 0;

            pool = new SemaphoreSlim(maxPoolSize, maxPoolSize);
            bag = new ConcurrentBag<TResource>();
        }

        protected abstract Task<TResource> AcquireNewResourceAsync();

        protected abstract bool IsResourceBroken(TResource resource);

        public async Task<PooledResource<TResource>> GetAsync()
        {
            return await GetAsync(PoolTimeout).ConfigureAwait(false);
        }

        public async Task<PooledResource<TResource>> GetAsync(TimeSpan poolTimeout)
        {
            if (await pool.WaitAsync(poolTimeout).ConfigureAwait(false))
            {
                TResource resource;

                // 1. Try taking one from existing - if available
                int current;
                while ((current = currentFreeCount) > 0)
                {
                    if (Interlocked.CompareExchange(ref currentFreeCount, current - 1, current) == current)
                    {
                        while (!bag.TryTake(out resource)) { }

                        return new PooledResource<TResource>(resource, ReturnAndRelease);
                    }
                }

                // 2. Else grow (increment) pool by one
                try
                {
                    resource = await AcquireNewResourceAsync().ConfigureAwait(false);
                    Interlocked.Increment(ref currentPoolSize);
                    logger.Info("{0}: Created new pooled resource", this);

                    return new PooledResource<TResource>(resource, ReturnAndRelease);
                }
                catch (Exception e)
                {
                    pool.Release();
                    throw new Exception(string.Format("{0}: error acquiring new resource - {1}", this, e.Message), e);
                }
            }
            else
            {
                throw new TimeoutException(string.Format("{0}: timeout ({1}) waiting to get PooledResource (pool busy)", this, poolTimeout));
            }
        }

        void ReturnAndRelease(TResource resource)
        {
            try 
            {
                if (IsResourceBroken(resource))
                {
                    Interlocked.Decrement(ref currentPoolSize);
                    logger.Warn("{0} discarded broken resource", this);
                }
                else
                {
                    Interlocked.Increment(ref currentFreeCount);
                    bag.Add(resource);
                }
            }
            finally
            {
                pool.Release();
            }
        }

        public override string ToString()
        {
            return string.Format("{0}({1}:{2}/{3})", GetType().Name, Label, CurrentFreeCount, CurrentPoolSize);
        }
    }
}
