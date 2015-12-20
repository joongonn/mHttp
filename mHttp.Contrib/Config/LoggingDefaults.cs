using NLog;
using NLog.Config;
using NLog.Targets;

namespace m.Config
{
    public static class LoggingDefaults
    {
        public const string Layout = @"${date:universalTime=true:format=yyyy-MM-ddTHH\:mm\:ss.fffZ} ${pad:padding=5:inner=${level:uppercase=true}} [${threadname}#${threadid}] - ${message}"; // ${callsite:className=false:fileName=true:includeSourcePath=false:methodName=false:cleanNamesOfAnonymousDelegates=true};

        public static LoggingConfiguration ToConsole(LogLevel level)
        {
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
    }
}
