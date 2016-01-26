using System;

namespace m.Utils
{
    public static class Time
    {
        public const string StringFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis { get { return (long)(DateTime.UtcNow - Epoch).TotalMilliseconds; } }

        public static long CurrentTimeSeconds { get { return (long)(DateTime.UtcNow - Epoch).TotalSeconds; } }

        public static int CurrentTimeHours { get { return (int)(DateTime.UtcNow - Epoch).TotalHours; } }

        public static long ToTimeMillis(this DateTime utcDateTime) => (long)(utcDateTime - Epoch).TotalMilliseconds;

        public static int ToTimeHours(this DateTime utcDateTime) => (int)(utcDateTime - Epoch).TotalHours;
    }
}
