using System;

namespace m.Utils
{
    public abstract class LifeCycleBase
    {
        readonly object thisLock = new object();
        volatile bool started = false;
        volatile bool shutdown = false;

        public bool IsStarted { get { return started; } }
        public bool IsShutdown { get { return shutdown; } }

        protected abstract void OnStart();
        protected abstract void OnShutdown();

        public bool Start()
        {
            lock (thisLock)
            {
                if (shutdown)
                {
                    throw new InvalidOperationException("Already shut down");
                }

                if (started)
                {
                    return false;
                }

                started = true;
                OnStart();
                return true;
            }
        }

        public bool Shutdown()
        {
            lock (thisLock)
            {
                if (!started)
                {
                    throw new InvalidOperationException("Not started");
                }

                if (shutdown)
                {
                    return false;
                }

                shutdown = true;
                OnShutdown();
                return true;
            }
        }
    }
}
