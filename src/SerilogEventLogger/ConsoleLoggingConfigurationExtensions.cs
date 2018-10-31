// Copyright 2015 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Serilog.Configuration;
using Serilog.Events;
using SerilogEventLogger.Sinks.Console;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.LiterateConsole() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class ConsoleLoggingConfigurationExtensions
    {
        const string DefaultOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {EventName} : {EventData}{NewLine}{Exception}";

        /// <summary>
        /// Writes log events to <see cref="System.Console"/>, using pretty printing to display inline event data.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="standardErrorFromLevel">Specifies the level at which events will be written to standard error.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration Console(
            this LoggerSinkConfiguration sinkConfiguration,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel? standardErrorFromLevel = null)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));
            return sinkConfiguration.Sink(new ConsoleSink(outputTemplate, formatProvider, standardErrorFromLevel), restrictedToMinimumLevel);
        }
    }
}
