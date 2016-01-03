namespace m.Utils
{
    sealed class LifeCycleToken : LifeCycleBase
    {
        public LifeCycleToken() { }

        protected override void OnStart() { }

        protected override void OnShutdown() { }
    }
}
