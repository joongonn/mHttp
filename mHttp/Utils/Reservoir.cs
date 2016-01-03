using System;
using System.Collections.Generic;
using System.Linq;

namespace m.Utils
{
    sealed class Reservoir<TValue>
    {
        readonly object sampleLock = new object();
        readonly Random rnd;
        readonly TValue[] reservoir;
        readonly IComparer<TValue> comparer;
        int n;

        public Reservoir(int size, IComparer<TValue> comparer)
        {
            rnd = new Random();
            reservoir = new TValue[size];
            this.comparer = comparer;
            Array.Clear(reservoir, 0, reservoir.Length);
            n = 0;
        }

        public void Sample(TValue value)
        {
            lock (sampleLock)
            {
                var k = reservoir.Length;

                if (n >= k)
                {
                    int r = rnd.Next(n);
                    if (r < k)
                    {
                        reservoir[r] = value;
                    }
                }
                else
                {
                    reservoir[n] = value;
                }

                n++;
            }
        }

        public TValue[] GetValues(params float[] percentiles)
        {
            if (percentiles.Any(p => p < 0f || 1.0f < p))
            {
                throw new ArgumentOutOfRangeException("percentiles", "percentile must be between 0f and 1.0f");
            }

            TValue[] sortedReservoir = reservoir.OrderBy(v => v, comparer).ToArray();
            var K = sortedReservoir.Length;
            var indexes = percentiles.Select(p => (p >= 1.0f) ? (K - 1) : (int)(p * K));

            return indexes.Select(index => sortedReservoir[index]).ToArray();
        }
    }
}
