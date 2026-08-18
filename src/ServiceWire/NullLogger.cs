namespace ServiceWire
{
    internal class NullLogger : ILog
    {
        public void Debug(string formattedMessage, params object[] args)
        {
        }

        public void Info(string formattedMessage, params object[] args)
        {
        }

        public void Warn(string formattedMessage, params object[] args)
        {
        }

        public void Error(string formattedMessage, params object[] args)
        {
        }

        public void Fatal(string formattedMessage, params object[] args)
        {
        }
    }

    internal static class LogExtensions
    {
        public static bool IsDebugEnabled(this ILog logger)
        {
            if (logger == null || logger is NullLogger) return false;
            var serviceWireLogger = logger as Logger;
            return serviceWireLogger == null || serviceWireLogger.LogLevel >= LogLevel.Debug;
        }
    }
}
