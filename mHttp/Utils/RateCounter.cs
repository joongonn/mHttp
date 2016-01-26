using System;

namespace m.Utils
{
    class RateCounter
    {
        readonly int resolutionMs;
        readonly long[] timeSlots;
        readonly int[] counts;

        public int MaxRate { get; private set; }

        public RateCounter(int resolutionMs)
        {
            MaxRate = 0;

            this.resolutionMs = resolutionMs;
            var slots = 1000 / resolutionMs;

            timeSlots = new long[slots];
            counts = new int[slots];
            Array.Clear(timeSlots, 0, slots);
            Array.Clear(counts, 0, slots);
        }

        public void Count(long timeMillis, int count)
        {
            var now = Time.CurrentTimeMillis;

            if (now - timeMillis < 1000)
            {
                timeMillis = timeMillis - (timeMillis % resolutionMs);
                var slot = (int)(timeMillis % 1000) / resolutionMs;

                lock (timeSlots)
                {
                    if (timeMillis > timeSlots[slot])
                    {
                        timeSlots[slot] = timeMillis;
                        counts[slot] = count;
                    }
                    else
                    {
                        counts[slot] += count;
                    }

                    var currentRate = 0;
                    for (int i=0; i<timeSlots.Length; i++)
                    {
                        if (now - timeSlots[i] <= 1000)
                        {
                            currentRate += counts[i];
                        }
                    }
                    if (currentRate > MaxRate)
                    {
                        MaxRate = currentRate;
                    }
                }
            }
        }

        public int GetCurrentRate()
        {
            var now = Time.CurrentTimeMillis;
            var currentRate = 0;

            for (int i=0; i<timeSlots.Length; i++)
            {
                if (now - timeSlots[i] <= 1000)
                {
                    currentRate += counts[i];
                }
            }

            return currentRate;
        }
    }
}

