using SerilogEventLogger.Serilog.Parameters;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SerilogEventLogger
{
    public class EventLogger<T> : EventLogger, IEventLogger<T>
    {
        public static string BuildLoggerName(Type loggerType)
        {
            return loggerType.GetTypeInfo().UnderlyingSystemType.ToString();
        }

        public EventLogger(
            ILogger parentLogger
        ) : base(BuildLoggerName(typeof(T)), parentLogger)
        {
        }
    }

    public class EventLogger : IEventLogger, IDisposable
    {
        internal const string SourceContext = "SourceContext";

        public const string EventDataTemplate = "{@EventData}";
        private static readonly PropertyValueConverter valueConverter =
            new PropertyValueConverter();
        private static readonly MessageTemplate eventDataTemplate =
            new MessageTemplateParser().Parse(EventDataTemplate);

        private readonly string name;
        private readonly ILogger innerLogger;

        public EventLogger(
            string name,
            ILogger parentLogger
        )
        {
            this.name = name;
            this.innerLogger = parentLogger.ForContext(SourceContext, name);
        }

        public IDisposable BeginScope(object state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return LogScope.Push(name, state);
        }

        public void LogEvent(string eventName, object data = null)
        {
            Log(new Event(eventName, data, Severity.Information));
        }

        public void LogEvent(string eventName, Severity severity, object data = null)
        {
            Log(new Event(eventName, data, severity));
        }

        public void LogEvent(string eventName, Exception exception, Severity severity = Severity.Error, object data = null)
        {
            Log(new Event(eventName, data, severity), exception);
        }

        public void Count(string counterName, int count = 1, object data = null)
        {
            Log(new Event(counterName, data).With("Count", count));
        }

        public IScopedMetric MoveGauge(string gaugeName, int delta = 1, object data = null)
        {
            var evt = new Event(gaugeName, data).With("Delta", delta);

            // Log initial move
            Log(evt);

            return new ScopedMetric(
                // Log reset back to original value
                additionalData => Log(evt
                    .With("Delta", delta * -1)
                    .With(additionalData)
                )
            );
        }

        public void SetGauge(string gaugeName, int value, object data = null)
        {
            Log(new Event(gaugeName, data).With("Value", value));
        }

        public IScopedMetric StartTimer(string timerName, object data = null)
        {
            var startTime = DateTime.Now;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            return new ScopedMetric(
                additionalData => Log(new Event(timerName, data)
                    .With("StartTime", startTime)
                    .With("ElapsedMilliseconds", stopwatch.ElapsedMilliseconds)
                    .With(additionalData)
                )
            );
        }

        internal void Log(
            Event logEvent,
            Exception ex = null
        )
        {
            var scopeData = FlattenScopeData(LogScope.Current);

            innerLogger
                .ForContext("EventName", logEvent.Name)
                .Write(new LogEvent(
                    DateTimeOffset.Now,
                    logEvent.Severity.ToLogEventLevel(),
                    ex,
                    eventDataTemplate,
                    logEvent.Data.ToSerilogProperties(valueConverter)
                        .Concat(scopeData.ToSerilogProperties(valueConverter))
                ));
        }

        private EventData FlattenScopeData(LogScope current)
        {
            var data = new EventData();
            while (current != null)
            {
                data.AddValues(current.State);

                current = current.Parent;
            }

            return data;
        }

        public void Dispose()
        {
            if (innerLogger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
            }
        }
    }
}
