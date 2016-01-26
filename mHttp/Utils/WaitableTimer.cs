using System;
using System.Diagnostics;
using System.Threading;

using m.Logging;

namespace m.Utils
{
    sealed class WaitableTimer : LifeCycleBase
    {
        public sealed class Job
        {
            public string Name { get; }
            readonly Action callback;

            public Job(string name, Action callback)
            {
                Name = name;
                this.callback = callback;
            }

            public void Run()
            {
                callback();
            }
        }

        readonly LoggingProvider.ILogger logger = LoggingProvider.GetLogger(typeof(WaitableTimer));

        public TimeSpan Period { get; }
        readonly Job[] jobs;
        readonly AutoResetEvent evt;
        readonly string toString;

        #region Diagnostics
        long signalsReceived = 0;
        long timeouts = 0;
        #endregion

        public WaitableTimer(string name, TimeSpan period, Job[] jobs)
        {
            Period = period;
            this.jobs = jobs;
            evt = new AutoResetEvent(false);
            toString = string.Format("{0}@{1}Hz", name, (1000 / Period.TotalMilliseconds));
        }

        public bool Signal()
        {
            return evt.Set();
        }

        protected override void OnStart()
        {
            var thread = new Thread(Loop)
            {
                Name = ToString(),
                IsBackground = false
            };

            thread.Start();
        }

        protected override void OnShutdown() { }

        void Loop()
        {
            logger.Info("{0} with {1} jobs started", this, jobs.Length);

            var stopWatch = new Stopwatch();
            var jobsTimeMs = 0d;

            while (!IsShutdown)
            {
                var waitFor = (int)(Period.TotalMilliseconds - jobsTimeMs);
                if (waitFor > 0)
                {
                    if (evt.WaitOne(waitFor))
                    {
                        signalsReceived++;
                    }
                    else
                    {
                        timeouts++;
                    }
                }

                jobsTimeMs = 0;
                stopWatch.Restart();
                RunJobs();
                stopWatch.Stop();
                jobsTimeMs += stopWatch.Elapsed.TotalMilliseconds;
            }

            RunJobs();

            logger.Info("{0} stopped", this);
        }

        void RunJobs()
        {
            for (int i=0; i<jobs.Length; i++)
            {
                try
                {
                    jobs[i].Run();
                }
                catch (Exception e)
                {
                    logger.Fatal("WaitableTimer.Job:[{0}] exception - {1}", jobs[i].Name, e.ToString());
                }
            }
        }

        public override string ToString() => toString;
    }
}
