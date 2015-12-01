namespace m.Utils
{
    public sealed class LifeCycleToken : LifeCycleBase
    {
        public LifeCycleToken() { }

        protected override void OnStart() { }

        protected override void OnShutdown() { }
    }
}
