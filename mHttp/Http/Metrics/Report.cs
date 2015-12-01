using System;

namespace m.Http.Metrics
{
    public class Report
    {
        public class Endpoint
        {
            public class Counter
            {
                public int StatusCode { get; set; }
                public int Count { get; set; }
            }

            public class ResponseTime
            {
                public float Percentile { get; set; }
                public float Value { get; set; }
            }

            public string Method { get; set; }
            public string Route { get; set; }
            public Counter[] Counters { get; set; }
            public ResponseTime[] ResponseTimes { get; set; }
        }

        public Endpoint[] Endpoints { get; set; }
    }
}
