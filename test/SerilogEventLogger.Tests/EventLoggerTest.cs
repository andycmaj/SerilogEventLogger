using System;
using FakeItEasy;
using Serilog;
using Xunit;

namespace SerilogEventLogger.Tests
{
    public class EventLoggerTest
    {
        private readonly ILogger fakeParentLogger = A.Fake<ILogger>();

        [Fact]
        public void Can_Build_LoggerName_From_Simple_Type()
        {
            const string expectedLoggerName = "System.String";

            new EventLogger<string>(fakeParentLogger);

            A.CallTo(() =>
                fakeParentLogger.ForContext(
                    EventLogger.SourceContext,
                    expectedLoggerName,
                    false
                )
            ).MustHaveHappened();
        }

        [Fact]
        public void Can_Build_LoggerName_From_Generic_Type()
        {
            const string expectedLoggerName = "System.Tuple`2[System.String,System.Int32]";

            new EventLogger<Tuple<string, int>>(fakeParentLogger);

            A.CallTo(() =>
                fakeParentLogger.ForContext(
                    EventLogger.SourceContext,
                    expectedLoggerName,
                    false
                )
            ).MustHaveHappened();
        }
    }
}
