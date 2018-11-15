using System;
using System.Globalization;
using System.IO;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using SerilogEventLogger;
using SerilogEventLogger.Sinks.Console;
using Xunit;

namespace SerilogEventLogger.Tests
{
    public class ConsoleSinkTest
    {
        private readonly TextWriter writer = new StringWriter();

        private readonly ILogger innerLogger;

        public ConsoleSinkTest()
        {
            innerLogger = new LoggerConfiguration()
                .WriteTo.Sink(new ConsoleSink("{Event}", null, writer))
                .CreateLogger();
        }

        [Fact]
        public void Can_Log_Json_To_Console()
        {
            var stamp = DateTimeOffset.UtcNow;
            var logger = new EventLogger("test", innerLogger);

            logger.Log(new Event("UniqueEventName", new { Foo = 42 }, stamp: stamp.DateTime));

            var output = writer.ToString();

            output.Should().Match("{\"time\":\"*\",\"event\":{\"Severity\":\"Information\",\"EventName\":\"UniqueEventName\",\"LoggerName\":\"test\",\"Data\":{\"Foo\":42}}}");
        }
    }
}