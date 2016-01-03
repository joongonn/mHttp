using System;

namespace m.Utils
{
    class RateCounter
    {
        readonly int resolutionMs;
        public readonly long[] times;
        public readonly int[] counts;

        public RateCounter(int resolutionMs)
        {
            this.resolutionMs = resolutionMs;
            var slots = 1000 / resolutionMs;

            times = new long[slots];
            counts = new int[slots];
            Array.Clear(times, 0, slots);
            Array.Clear(counts, 0, slots);
        }

        public void Count(long timeMillis, int count)
        {
            var now = Time.CurrentTimeMillis;

            if (now - timeMillis < 1000)
            {
                var remainder = timeMillis % resolutionMs;
                timeMillis = timeMillis - remainder;
                int slot = (int)(timeMillis % 1000) / resolutionMs;

                if (timeMillis > times[slot])
                {
                    times[slot] = timeMillis;
                    counts[slot] = count;
                }
                else
                {
                    counts[slot] += count;
                }
            }
        }

        public int GetCurrentRate()
        {
            var now = Time.CurrentTimeMillis;
            var rate = 0;

            for (int slot=0; slot<times.Length; slot++)
            {
                if (now - times[slot] <= 1000)
                {
                    rate += counts[slot];
                }
            }

            return rate;
        }
    }
}

