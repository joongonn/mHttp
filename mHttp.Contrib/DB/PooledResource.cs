using System;
using System.Threading;

namespace m.DB
{
    public sealed class PooledResource<TResource> : IDisposable
    {
        const int False = 0;
        const int True  = 1;

        TResource resource;
        readonly Action<TResource> returnAndRelease;
        int isDisposed = False;

        public TResource Resource
        {
            get
            {
                if (isDisposed == False)
                {
                    return resource;
                }
                else
                {
                    throw new ObjectDisposedException("PooledResource", "Already disposed and returned to pool");
                }
            }
        }

        internal PooledResource(TResource resource, Action<TResource> returnAndRelease)
        {
            this.resource = resource;
            this.returnAndRelease = returnAndRelease;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, True) == False)
            {
                returnAndRelease(resource);
                resource = default(TResource);
            }
        }
    }
}
