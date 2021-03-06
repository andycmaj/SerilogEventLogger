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

        public LoggerConfiguration SerilogConfiguration { get; }

        public LoggingConfiguration(
            string environment,
            string applicationName = null,
            string applicationVersion = null,
            string serverName = null
        )
        {
            Environment = environment;
            ApplicationName = applicationName;
            ApplicationVersion = applicationVersion;
            ServerName = serverName;

            SerilogConfiguration = Configure();
        }

        private LoggerConfiguration Configure()
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
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "{Event}");

            if (!string.IsNullOrWhiteSpace(ApplicationVersion))
            {
                serilogConfig.Enrich.WithProperty("ApplicationVersion", ApplicationVersion);
            }
            if (!string.IsNullOrWhiteSpace(Environment))
            {
                serilogConfig.Enrich.WithProperty("Environment", Environment);
            }

            return serilogConfig;
        }
    }
}
