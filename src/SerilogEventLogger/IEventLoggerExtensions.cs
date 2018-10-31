using System;

namespace SerilogEventLogger
{
    public static class IEventLoggerExtensions
    {
        public static void InfoEvent(
            this IEventLogger logger,
            string eventName,
            object data = null
        )
        {
            logger.LogEvent(eventName, Severity.Information, data);
        }

        public static void DebugEvent(
            this IEventLogger logger,
            string eventName,
            object data = null
        )
        {
            logger.LogEvent(eventName, Severity.Debug, data);
        }

        public static void WarningEvent(
            this IEventLogger logger,
            string eventName,
            object data = null
        )
        {
            logger.LogEvent(eventName, Severity.Warning, data);
        }

        public static void WarningEvent(
            this IEventLogger logger,
            string eventName,
            Exception ex,
            object data = null
        )
        {
            logger.LogEvent(eventName, ex, Severity.Warning, data);
        }        

        public static void ErrorEvent(
            this IEventLogger logger,
            string eventName,
            object data = null
        )
        {
            logger.LogEvent(eventName, Severity.Error, data);
        }

        public static void ErrorEvent(
            this IEventLogger logger,
            string eventName,
            Exception ex,
            object data = null
        )
        {
            logger.LogEvent(eventName, ex, Severity.Error, data);
        }

        public static void AlertEvent(
            this IEventLogger logger,
            string eventName,
            object data = null
        )
        {
            logger.LogEvent(eventName, Severity.Alert, data);
        }

        public static void AlertEvent(
            this IEventLogger logger,
            string eventName,
            Exception ex,
            object data = null
        )
        {
            logger.LogEvent(eventName, ex, Severity.Alert, data);
        }
    }
}
