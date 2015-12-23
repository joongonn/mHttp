using m.Logging;

public abstract class BaseTest
{
    static BaseTest()
    {
        LoggingProvider.Use(LoggingProvider.ConsoleLoggingProvider);
    }
}
