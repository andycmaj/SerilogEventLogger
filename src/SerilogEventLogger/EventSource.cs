namespace SerilogEventLogger
{
    public class EventSource
    {
        public string Environment { get; set; }
        public string ApplicationName { get; set;  }
        public string ApplicationVersion { get; set; }
        public string Host { get; set; }

        public EventSource() { }

        public EventSource(LoggingConfiguration loggingConfig)
        {
            Environment = loggingConfig.Environment;
            ApplicationName = loggingConfig.ApplicationName;
            ApplicationVersion = loggingConfig.ApplicationVersion;
            Host = loggingConfig.ServerName;
        }
    }
}
