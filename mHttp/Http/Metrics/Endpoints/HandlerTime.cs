using System.Collections.Generic;

using m.Utils;

namespace m.Http.Metrics.Endpoints
{
    sealed class HandlerTime
    {
        readonly Reservoir<float> reservoir;

        public HandlerTime(int size)
        {
            reservoir = new Reservoir<float>(size, Comparer<float>.Default);
        }

        public void Update(IEnumerable<RequestLogs.Log> logs)
        {
            foreach (var log in logs)
            {
                reservoir.Sample((float)(log.CompletedOn - log.ArrivedOn).TotalMilliseconds);
            }
        }

        public float[] GetTimes(params float[] percentiles)
        {
            return reservoir.GetValues(percentiles);
        }
    }
}
