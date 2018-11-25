using System;
using System.Collections.Generic;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;
using Xunit;

namespace SerilogEventLogger.Tests
{
    public class EventTest : IDisposable
    {
        private readonly ILogger innerLogger =
            new LoggerConfiguration().WriteTo.Sink(new TestCorrelatorSink()).CreateLogger();

        private readonly IDisposable currentContext = TestCorrelator.CreateContext();

        ///             innerLogger
        // .ForContext("EventName", logEvent.Name)
        // .Write(new LogEvent(
        //     DateTimeOffset.Now,
        //     logEvent.Severity.ToLogEventLevel(),
        //     ex,
        //     eventDataTemplate,
        //     logEvent.Data.ToSerilogProperties(valueConverter)
        //         .Concat(scopeData.ToSerilogProperties(valueConverter))
        // ));

        [Fact]
        public void Can_Flatten_Deep_Anon_EventData()
        {
            var logger = new EventLogger("test", innerLogger);

            var eventData = new
            {
                Foo = "foo",
                Bar = 42,
                Baz = new
                {
                    Quux = "bazquux"
                }
            };

            logger.InfoEvent("event", eventData);

            AssertShouldContainProperty("Foo", "foo");
            AssertShouldContainProperty("Bar", 42);
            AssertShouldContainProperty("Baz.Quux", "bazquux");
        }

        [Fact]
        public void Can_Flatten_Array_EventData()
        {
            var logger = new EventLogger("test", innerLogger);

            var eventData = new
            {
                Foo = new[] { 1, 2, 3 },
                Bar = new
                {
                    Baz = new[] { 42 }
                }
            };

            logger.InfoEvent("event", eventData);

            AssertShouldContainProperty("Foo[0]", 1);
            AssertShouldContainProperty("Foo[1]", 2);
            AssertShouldContainProperty("Foo[2]", 3);
            AssertShouldContainProperty("Bar.Baz[0]", 42);
        }

        private void AssertShouldContainProperty(string key, object value)
        {
            LogEventPropertyValue scalarPropertyValue = new ScalarValue(value);
            TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().ContainSingle()
                .Which.Properties
                .Should().Contain(
                    KeyValuePair.Create(key, scalarPropertyValue)
                );
        }

        public void Dispose()
        {
            currentContext.Dispose();
        }
    }
}