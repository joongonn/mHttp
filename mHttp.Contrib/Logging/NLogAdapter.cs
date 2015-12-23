using System;

using NLog;
using NLog.Config;
using NLog.Targets;

namespace m.Logging
{
    public class NLogAdapter : LoggingProvider.ILogger
    {
        public static LoggingConfiguration ToConsole(LogLevel level)
        {
            // public const string Layout = @"${date:universalTime=true:format=yyyy-MM-ddTHH\:mm\:ss.fffZ} ${pad:padding=5:inner=${level:uppercase=true}} [${threadname}#${threadid}] - ${message}"; // ${callsite:className=false:fileName=true:includeSourcePath=false:methodName=false:cleanNamesOfAnonymousDelegates=true};
            const string Layout = @"${date:universalTime=true:format=yyyy-MM-ddTHH\:mm\:ss.fffZ} ${pad:padding=5:inner=${level:uppercase=true}} [${threadname}#${threadid}] - ${callsite:className=false:fileName=true:includeSourcePath=false:methodName=false:cleanNamesOfAnonymousDelegates=true} ${message}"; // ${callsite:className=false:fileName=true:includeSourcePath=false:methodName=false:cleanNamesOfAnonymousDelegates=true};

            var consoleTarget = new ConsoleTarget
            {
                Name = "console",
                Layout = Layout
            };

            var config = new LoggingConfiguration();
            config.AddTarget(consoleTarget);
            config.LoggingRules.Add(new LoggingRule("*", level, consoleTarget));

            return config;
        }
                
        public static readonly Func<Type, LoggingProvider.ILogger> Provider = type => new NLogAdapter(type);

        Logger logger;

        public NLogAdapter(Type type)
        {
            logger = NLog.LogManager.GetLogger(type.Name);
        }

        void Log(NLog.LogLevel level, string msg, params object[] args)
        {
            logger.Log(typeof(NLogAdapter), new NLog.LogEventInfo(level, logger.Name, null, msg, args));
        }

        public void Debug(string msg, params object[] args) { Log(NLog.LogLevel.Debug, msg, args); }
        public void Info(string msg, params object[] args)  { Log(NLog.LogLevel.Info, msg, args); }
        public void Warn(string msg, params object[] args)  { Log(NLog.LogLevel.Warn, msg, args); }
        public void Error(string msg, params object[] args) { Log(NLog.LogLevel.Error, msg, args); }
        public void Fatal(string msg, params object[] args) { Log(NLog.LogLevel.Fatal, msg, args); }
    }
}
