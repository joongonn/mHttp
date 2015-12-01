using NLog;

using m.Config;

public abstract class BaseTest
{
    static BaseTest()
    {
        LogManager.Configuration = LoggingDefaults.ToConsole(LogLevel.Debug);
    }
}
