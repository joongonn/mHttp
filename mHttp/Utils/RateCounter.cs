using System;

namespace m.Utils
{
    class RateCounter
    {
        readonly int resolutionMs;
        readonly long[] times;
        readonly int[] counts;

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
                int timeSlot = (int)(timeMillis % 1000) / resolutionMs;

                lock (times)
                {
                    if (timeMillis > times[timeSlot])
                    {
                        times[timeSlot] = timeMillis;
                        counts[timeSlot] = count;
                    }
                    else
                    {
                        counts[timeSlot] += count;
                    }
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

