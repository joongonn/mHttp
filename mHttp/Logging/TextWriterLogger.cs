using System;
using System.IO;
using System.Threading;

namespace m.Logging
{
    class TextWriterLogger : LoggingProvider.ILogger
    {
        readonly TextWriter writer;

        public TextWriterLogger(TextWriter writer)
        {
            this.writer = writer;
        }

        void Log(string level, string msg, params object[] args)
        {
            writer.WriteLine("{0} {1,5} [{2}#{3}] {4}",
                             DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                             level,
                             Thread.CurrentThread.Name,
                             Thread.CurrentThread.ManagedThreadId,
                             string.Format(msg, args));
        }

        public void Debug(string msg, params object[] args) { Log("DEBUG", msg, args); }
        public void Info(string msg, params object[] args)  { Log("INFO", msg, args); }
        public void Warn(string msg, params object[] args)  { Log("WARN", msg, args); }
        public void Error(string msg, params object[] args) { Log("ERROR", msg, args); }
        public void Fatal(string msg, params object[] args) { Log("FATAL", msg, args); }
    }
}
