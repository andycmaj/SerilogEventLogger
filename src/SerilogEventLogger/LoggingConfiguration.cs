using System;
using Serilog;
using Serilog.Debugging;

namespace SerilogEventLogger
{
    public class LoggingConfiguration
    {
        public string Environment { get; }
        public string ApplicationName { get; }
        public string ApplicationVersion { get; }
        public string ServerName { get; }
        public bool ShouldLogAsynchronously { get; }

        public bool ShouldLogToConsole { get; set; } = false;

        public LoggingConfiguration(
            string environment,
            string applicationName = null,
            string applicationVersion = null,
            string serverName = null,
            bool shouldLogAsynchronously = false
        )
        {
            Environment = environment;
            ApplicationName = applicationName;
            ApplicationVersion = applicationVersion;
            ServerName = serverName;
            ShouldLogAsynchronously = shouldLogAsynchronously;
        }

        public LoggerConfiguration Configure()
        {
            var eventSource =
                new EventSource {
                    ApplicationName = ApplicationName,
                    ApplicationVersion = ApplicationVersion,
                    Environment = Environment,
                    Host = ServerName
                };

            SelfLog.Enable(Console.Error);

            var serilogConfig =
                new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .Enrich.FromLogContext();

            if (!string.IsNullOrWhiteSpace(ApplicationVersion))
            {
                serilogConfig.Enrich.WithProperty("ApplicationVersion", ApplicationVersion);
            }
            if (!string.IsNullOrWhiteSpace(Environment))
            {
                serilogConfig.Enrich.WithProperty("Environment", Environment);
            }

            if (ShouldLogToConsole)
            {
                Console.WriteLine("Console logging enabled.");
                serilogConfig.WriteTo.Console();
            }

            return serilogConfig;
        }
    }
}
