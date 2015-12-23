namespace m.Logging
{
    class NullLoggerImpl : LoggingProvider.ILogger
    {
        public void Debug(string msg, params object[] args) { }
        public void Info(string msg, params object[] args) { }
        public void Warn(string msg, params object[] args) { }
        public void Error(string msg, params object[] args) { }
        public void Fatal(string msg, params object[] args) { }
    }
}
