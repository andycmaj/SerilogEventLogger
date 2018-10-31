// Copyright 2016 Serilog Contributors
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace SerilogEventLogger
{
    /// <summary>
    /// Renders log events into a default JSON format for consumption by Splunk.
    /// </summary>
    public class JsonEventFormatter : ITextFormatter
    {
        static readonly JsonValueFormatter ValueFormatter = new JsonValueFormatter();

        private static readonly string[] EventSourceProperties = new[]
        {
            "EventName",
            "SourceContext",
            "ApplicationVersion",
            "Environment"
        };

        readonly string suffix;
        readonly EventSource source;

        /// <summary>
        /// Construct a <see cref="SplunkJsonFormatter"/>.
        /// </summary>
        /// <param name="index">The Splunk index to log to</param>
        /// <param name="source">The source of the event</param>
        public JsonEventFormatter(
            EventSource source,
            string index
        )
        {
            this.source = source;

            var suffixWriter = new StringWriter();
            suffixWriter.Write("}"); // Terminates "event"

            if (!string.IsNullOrWhiteSpace(source.ApplicationName))
            {
                suffixWriter.Write(",\"source\":");
                JsonValueFormatter.WriteQuotedJsonString(source.ApplicationName, suffixWriter);

                suffixWriter.Write(",\"sourcetype\":");
                JsonValueFormatter.WriteQuotedJsonString("httpevent", suffixWriter);
            }

            if (!string.IsNullOrWhiteSpace(source.Host))
            {
                suffixWriter.Write(",\"host\":");
                JsonValueFormatter.WriteQuotedJsonString(source.Host, suffixWriter);
            }

            if (!string.IsNullOrWhiteSpace(index))
            {
                suffixWriter.Write(",\"index\":");
                JsonValueFormatter.WriteQuotedJsonString(index, suffixWriter);
            }
            suffixWriter.Write('}'); // Terminates the payload
            suffix = suffixWriter.ToString();
        }

        public JsonEventFormatter() : this(new EventSource(), null)
        {
        }

        /// <inheritdoc/>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));

            output.Write("{\"time\":\"");
            output.Write(logEvent.Timestamp.ToEpoch().ToString(CultureInfo.InvariantCulture));
            output.Write("\",\"event\":{\"Severity\":\"");
            output.Write(logEvent.Level);
            output.Write('"');

            // Configure common event source properties
            LogEventPropertyValue eventName = null;
            foreach (var eventSourcePropertyName in EventSourceProperties)
            {
                if (logEvent.Properties.TryGetValue(eventSourcePropertyName, out eventName))
                {
                    if (eventSourcePropertyName == "SourceContext")
                    {
                        output.Write($",\"LoggerName\":");
                    }
                    else
                    {
                        output.Write($",\"{eventSourcePropertyName}\":");
                    }
                    ValueFormatter.Format(eventName, output);
                }
            }

            if (logEvent.Exception != null)
            {
                output.Write(",\"Exception\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            var propertiesToRender = logEvent.Properties.Where(IsRenderedProperty);
            if (propertiesToRender.Count() != 0)
            {
                WriteProperties(propertiesToRender, output);
            }

            var message = logEvent.RenderMessage(null);
            if (!(string.IsNullOrWhiteSpace(message) || message == EventLogger.EventDataTemplate))
            {
                output.Write(",\"Message\":");
                JsonValueFormatter.WriteQuotedJsonString(message, output);
            }

            output.WriteLine(suffix);
        }

        static bool IsRenderedProperty(KeyValuePair<string, LogEventPropertyValue> property)
        {
            return !EventSourceProperties.Contains(property.Key);
        }

        static void WriteProperties(
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties,
            TextWriter output
        )
        {
            output.Write(",\"Data\":{");

            var precedingDelimiter = "";
            foreach (var property in properties.Where(IsRenderedProperty))
            {
                StructureValue structure = property.Value as StructureValue;
                if (structure != null)
                {
                    FlattenAndWriteStructure(structure, output, ref precedingDelimiter);
                }
                else
                {
                    output.Write(precedingDelimiter);
                    precedingDelimiter = ",";

                    JsonValueFormatter.WriteQuotedJsonString(property.Key, output);
                    output.Write(':');
                    ValueFormatter.Format(property.Value, output);
                }
            }

            output.Write('}');
        }

        static void FlattenAndWriteStructure(StructureValue structure, TextWriter output, ref string precedingDelimiter)
        {
            foreach (var property in structure.Properties)
            {
                var nestedStructure = property.Value as StructureValue;
                if (nestedStructure != null)
                {
                    FlattenAndWriteStructure(nestedStructure, output, ref precedingDelimiter);
                }
                else
                {
                    output.Write(precedingDelimiter);
                    precedingDelimiter = ",";

                    JsonValueFormatter.WriteQuotedJsonString(property.Name, output);
                    output.Write(':');
                    ValueFormatter.Format(property.Value, output);
                }
            }
        }
    }
}
