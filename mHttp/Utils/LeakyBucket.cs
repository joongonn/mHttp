using System;
using System.Threading;

namespace m.Utils
{
    public sealed class LeakyBucket
    {
        public readonly int Capacity;
        public readonly int LeakRate;

        int currentSize;

        readonly object leakLock = new object();
        DateTime lastLeakOn;

        public LeakyBucket(int capacity, int leaksPerSecond)
        {
            this.Capacity = capacity;
            this.LeakRate = leaksPerSecond;

            currentSize = 0;
            lastLeakOn = DateTime.UtcNow;
        }

        public bool Fill(int amount)
        {
            var current = Thread.VolatileRead(ref currentSize);
            var fillTo = current + amount;
            while (fillTo <= Capacity)
            {
                if (Interlocked.CompareExchange(ref currentSize, fillTo, current) == current)
                {
                    return true;
                }

                current = currentSize;
            }

            return false;
        }

        public void Leak()
        {
            lock (leakLock)
            {
                var current = currentSize;
                if (current == 0)
                {
                    lastLeakOn = DateTime.UtcNow;
                    return;
                }
                else
                {
                    var elapsed = (DateTime.UtcNow - lastLeakOn).TotalMilliseconds;
                    var leak = (elapsed / 1000) * LeakRate;
                    if (leak >= 1)
                    {
                        while (Interlocked.CompareExchange(ref currentSize, Math.Max(0, current - (int)leak), current) != current)
                        {
                            current = currentSize;
                            elapsed = (DateTime.UtcNow - lastLeakOn).TotalMilliseconds;
                            leak = (elapsed / 1000) * LeakRate;
                        }

                        lastLeakOn = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}
