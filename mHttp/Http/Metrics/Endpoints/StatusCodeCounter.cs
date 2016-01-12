using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace m.Http.Metrics.Endpoints
{
    sealed class StatusCodeCounter : IEnumerable<StatusCodeCounter.Entry>
    {
        public struct Entry
        {
            public readonly int Code;
            public readonly int Count;

            public Entry(int code, int count)
            {
                Code = code;
                Count = count;
            }
        }

        readonly int[] cummulativeCountsByCodeIndex;

        public StatusCodeCounter()
        {
            cummulativeCountsByCodeIndex = new int[500];
            Array.Clear(cummulativeCountsByCodeIndex, 0, cummulativeCountsByCodeIndex.Length);
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            return cummulativeCountsByCodeIndex.Select((count, codeIndex) => new Entry(codeIndex + 100, count)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Update(IEnumerable<RequestLogs.Log> logs)
        {
            foreach (var log in logs)
            {
                var statusCodeIndex = (int)log.Response.StatusCode - 100;
                cummulativeCountsByCodeIndex[statusCodeIndex]++;
            }
        }
    }
}
