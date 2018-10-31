using System;
using Serilog.Events;

namespace SerilogEventLogger
{
    internal static class SeverityExtensions
    {
        internal static LogEventLevel ToLogEventLevel(this Severity severity)
        {
            switch (severity)
            {
                case Severity.Debug:
                    return LogEventLevel.Debug;

                case Severity.Information:
                    return LogEventLevel.Information;

                case Severity.Warning:
                    return LogEventLevel.Warning;

                case Severity.Error:
                    return LogEventLevel.Error;

                case Severity.Alert:
                    return LogEventLevel.Fatal;

                default:
                    throw new NotImplementedException($"{severity} does not map to any LogLevel");
            }
        }
    }
}
