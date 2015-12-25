using System.Threading;

namespace m.Http.Metrics
{
    class BackendMetrics
    {
        internal long [][] totalRequestBytesIn;
        internal long [][] totalResponseBytesOut;

        public BackendMetrics(Router router)
        {
            var routeTables = router.Length;
            totalRequestBytesIn = new long[routeTables][];
            totalResponseBytesOut = new long[routeTables][];

            for (int i=0; i<routeTables; i++)
            {
                var endpoints = router[i].Length;
                totalRequestBytesIn[i] = new long[endpoints];
                totalResponseBytesOut[i] = new long[endpoints];

                for (int j=0; j<endpoints; j++)
                {
                    totalRequestBytesIn[i][j] = 0;
                    totalResponseBytesOut[i][j] = 0;
                }
            }
        }

        public void CountRequestBytesIn(int routeTableIndex, int endpointIndex, int requestBytes)
        {
            Interlocked.Add(ref totalRequestBytesIn[routeTableIndex][endpointIndex], requestBytes);
        }

        public void CountResponseBytesOut(int routeTableIndex, int endpointIndex, int responseBytes)
        {
            Interlocked.Add(ref totalResponseBytesOut[routeTableIndex][endpointIndex], responseBytes);
        }

        public void CountBytes(int routeTableIndex, int endpointIndex, int requestBytes, int responseBytes)
        {
            Interlocked.Add(ref totalRequestBytesIn[routeTableIndex][endpointIndex], requestBytes);
            Interlocked.Add(ref totalResponseBytesOut[routeTableIndex][endpointIndex], responseBytes);
        }
    }
}
