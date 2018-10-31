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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImsHealth.Logging.Sinks.Console;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using SysConsole = System.Console;

namespace SerilogEventLogger.Sinks.Console
{
    internal class ConsoleSink : ILogEventSink
    {
        const ConsoleColor Text = ConsoleColor.White,
                           Subtext = ConsoleColor.Gray,
                           Punctuation = ConsoleColor.DarkGray,

                           VerboseLevel = ConsoleColor.Gray,
                           DebugLevel = VerboseLevel,
                           InformationLevel = ConsoleColor.White,
                           WarningLevel = ConsoleColor.Yellow,
                           ErrorLevel = ConsoleColor.Red,
                           FatalLevel = ErrorLevel,

                           KeywordSymbol = ConsoleColor.Blue,
                           NumericSymbol = ConsoleColor.Magenta,
                           StringSymbol = ConsoleColor.Cyan,
                           OtherSymbol = ConsoleColor.Green,
                           NameSymbol = Subtext,
                           RawText = ConsoleColor.Yellow;

        const string StackFrameLinePrefix = "   ";

        class LevelFormat
        {
            public LevelFormat(ConsoleColor color)
            {
                Color = color;
            }

            public ConsoleColor Color { get; }
        }

        readonly IDictionary<LogEventLevel, LevelFormat> _levels = new Dictionary<LogEventLevel, LevelFormat>
        {
            { LogEventLevel.Verbose, new LevelFormat(VerboseLevel) },
            { LogEventLevel.Debug, new LevelFormat(DebugLevel) },
            { LogEventLevel.Information, new LevelFormat(InformationLevel) },
            { LogEventLevel.Warning, new LevelFormat(WarningLevel) },
            { LogEventLevel.Error, new LevelFormat(ErrorLevel) },
            { LogEventLevel.Fatal, new LevelFormat(FatalLevel) },
        };

        readonly IFormatProvider _formatProvider;
        readonly object _syncRoot = new object();
        readonly MessageTemplate _outputTemplate;
        readonly LogEventLevel? _standardErrorFromLevel;
        readonly JsonEventFormatter _jsonFormatter;

        public ConsoleSink(
            string outputTemplate,
            IFormatProvider formatProvider,
            LogEventLevel? standardErrorFromLevel)
        {
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));
            _outputTemplate = new MessageTemplateParser().Parse(outputTemplate);
            _formatProvider = formatProvider;
            _standardErrorFromLevel = standardErrorFromLevel;
            _jsonFormatter = new JsonEventFormatter();
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            var outputProperties = OutputProperties.GetOutputProperties(logEvent);
            var outputStream = GetOutputStream(logEvent.Level);
            lock (_syncRoot)
            {
                try
                {
                    foreach (var outputToken in _outputTemplate.Tokens)
                    {
                        var propertyToken = outputToken as PropertyToken;
                        if (propertyToken == null)
                        {
                            RenderOutputTemplateTextToken(outputToken, outputProperties, outputStream);
                        }
                        else switch (propertyToken.PropertyName)
                        {
                            case OutputProperties.LevelPropertyName:
                                RenderLevelToken(logEvent.Level, outputToken, outputProperties, outputStream);
                                break;
                            case OutputProperties.MessagePropertyName:
                                RenderMessageToken(logEvent, outputStream);
                                break;
                            case OutputProperties.ExceptionPropertyName:
                                RenderExceptionToken(propertyToken, outputProperties, outputStream);
                                break;
                            case "EventData":
                                RenderEventData(logEvent, outputStream);
                                break;
                            default:
                                RenderOutputTemplatePropertyToken(propertyToken, outputProperties, outputStream);
                                break;
                        }
                    }
                }
                finally { SysConsole.ResetColor(); }
            }
        }

        void RenderEventData(LogEvent logEvent, TextWriter outputStream)
        {
            _jsonFormatter.Format(logEvent, outputStream);
        }

        void RenderExceptionToken(
            PropertyToken outputToken,
            IReadOnlyDictionary<string, LogEventPropertyValue> outputProperties,
            TextWriter outputStream)
        {
            var sw = new StringWriter();
            outputToken.Render(outputProperties, sw, _formatProvider);
            var lines = new StringReader(sw.ToString());
            string nextLine;
            while ((nextLine = lines.ReadLine()) != null)
            {
                SysConsole.ForegroundColor = nextLine.StartsWith(StackFrameLinePrefix) ? Subtext : Text;
                outputStream.WriteLine(nextLine);
            }
        }

        void RenderOutputTemplatePropertyToken(
            PropertyToken outputToken,
            IReadOnlyDictionary<string, LogEventPropertyValue> outputProperties,
            TextWriter outputStream)
        {
            SysConsole.ForegroundColor = Subtext;

            // This code is shared with MessageTemplateFormatter in the core Serilog
            // project. Its purpose is to modify the way tokens are formatted to
            // use "output template" rather than "message template" rules.

            // First variation from normal rendering - if a property is missing,
            // don't render anything (message templates render the raw token here).
            LogEventPropertyValue propertyValue;
            if (!outputProperties.TryGetValue(outputToken.PropertyName, out propertyValue))
                return;

            // Second variation; if the value is a scalar string, use literal
            // rendering and support some additional formats: 'u' for uppercase
            // and 'w' for lowercase.
            var sv = propertyValue as ScalarValue;
            if (sv?.Value is string)
            {
                var overridden = new Dictionary<string, LogEventPropertyValue>
                {
                    { outputToken.PropertyName, new LiteralStringValue((string) sv.Value) }
                };

                outputToken.Render(overridden, outputStream, _formatProvider);
            }
            else
            {
                outputToken.Render(outputProperties, outputStream, _formatProvider);
            }
        }

        void RenderLevelToken(
            LogEventLevel level,
            MessageTemplateToken token,
            IReadOnlyDictionary<string, LogEventPropertyValue> properties,
            TextWriter outputStream)
        {
            LevelFormat format;
            if (!_levels.TryGetValue(level, out format))
                format = _levels[LogEventLevel.Warning];

            SysConsole.ForegroundColor = format.Color;

            if (level == LogEventLevel.Error || level == LogEventLevel.Fatal)
            {
                SysConsole.BackgroundColor = format.Color;
                SysConsole.ForegroundColor = ConsoleColor.White;
            }

            token.Render(properties, outputStream);
            SysConsole.ResetColor();
        }

        void RenderOutputTemplateTextToken(
            MessageTemplateToken outputToken,
            IReadOnlyDictionary<string, LogEventPropertyValue> outputProperties,
            TextWriter outputStream)
        {
            SysConsole.ForegroundColor = Punctuation;
            outputToken.Render(outputProperties, outputStream, _formatProvider);
        }

        void RenderMessageToken(LogEvent logEvent, TextWriter outputStream)
        {
            foreach (var messageToken in logEvent.MessageTemplate.Tokens)
            {
                var messagePropertyToken = messageToken as PropertyToken;
                if (messagePropertyToken != null)
                {
                    LogEventPropertyValue value;
                    if (!logEvent.Properties.TryGetValue(messagePropertyToken.PropertyName, out value))
                    {
                        SysConsole.ForegroundColor = RawText;
                        outputStream.Write(messagePropertyToken);
                    }
                    else
                    {
                        var scalar = value as ScalarValue;
                        if (scalar != null)
                        {
                            SysConsole.ForegroundColor = GetScalarColor(scalar);

                            if (scalar.Value is string && messagePropertyToken.Format == null && messagePropertyToken.Alignment == null)
                                outputStream.Write(scalar.Value);
                            else if (scalar.Value is bool && messagePropertyToken.Format == null && messagePropertyToken.Alignment == null)
                                outputStream.Write(scalar.Value.ToString().ToLowerInvariant());
                            else
                                messagePropertyToken.Render(logEvent.Properties, outputStream, _formatProvider);
                        }
                        else
                        {
                            PrettyPrint(value, messagePropertyToken.Format, _formatProvider, outputStream);
                        }
                    }
                }
                else
                {
                    SysConsole.ForegroundColor = Text;
                    messageToken.Render(logEvent.Properties, outputStream, _formatProvider);
                }
            }
        }

        void PrettyPrint(
            LogEventPropertyValue value,
            string format,
            IFormatProvider formatProvider,
            TextWriter outputStream)
        {
            var scalar = value as ScalarValue;
            if (scalar != null)
            {
                SysConsole.ForegroundColor = GetScalarColor(scalar);
                value.Render(outputStream, format, formatProvider);
                return;
            }

            var seq = value as SequenceValue;
            if (seq != null)
            {
                SysConsole.ForegroundColor = Punctuation;
                outputStream.Write("[");

                var sep = "";
                foreach (var element in seq.Elements)
                {
                    SysConsole.ForegroundColor = Punctuation;
                    outputStream.Write(sep);
                    sep = ", ";

                    PrettyPrint(element, null, formatProvider, outputStream);
                }

                SysConsole.ForegroundColor = Punctuation;
                outputStream.Write("]");
                return;
            }

            var str = value as StructureValue;
            if (str != null)
            {
                if (str.TypeTag != null)
                {
                    SysConsole.ForegroundColor = Subtext;
                    outputStream.Write(str.TypeTag);
                    outputStream.Write(" ");
                }

                SysConsole.ForegroundColor = Punctuation;
                outputStream.Write("{");

                var sep = "";
                foreach (var prop in str.Properties)
                {
                    SysConsole.ForegroundColor = Punctuation;
                    outputStream.Write(sep);
                    sep = ", ";

                    SysConsole.ForegroundColor = NameSymbol;
                    outputStream.Write(prop.Name);

                    SysConsole.ForegroundColor = Punctuation;
                    outputStream.Write("=");

                    PrettyPrint(prop.Value, null, formatProvider, outputStream);
                }

                SysConsole.ForegroundColor = Punctuation;
                outputStream.Write("}");
                return;
            }

            var div = value as DictionaryValue;
            if (div != null)
            {
                SysConsole.ForegroundColor = Punctuation;
                outputStream.Write("{");

                var sep = "";
                foreach (var element in div.Elements)
                {
                    SysConsole.ForegroundColor = Punctuation;
                    outputStream.Write(sep);
                    sep = ", ";
                    outputStream.Write("[");
                    PrettyPrint(element.Key, null, formatProvider, outputStream);

                    SysConsole.ForegroundColor = Punctuation;
                    outputStream.Write("]=");

                    PrettyPrint(element.Value, null, formatProvider, outputStream);
                }

                SysConsole.ForegroundColor = Punctuation;
                outputStream.Write("}");
                return;
            }

            value.Render(outputStream, format, formatProvider);
        }

        ConsoleColor GetScalarColor(ScalarValue scalar)
        {
            if (scalar.Value == null || scalar.Value is bool)
                return KeywordSymbol;

            if (scalar.Value is string)
                return StringSymbol;

            if (scalar.Value.GetType().GetTypeInfo().IsPrimitive || scalar.Value is decimal)
                return NumericSymbol;

            return OtherSymbol;
        }

        TextWriter GetOutputStream(LogEventLevel logLevel)
         {
            if (!_standardErrorFromLevel.HasValue)
            {
                return SysConsole.Out;
            }
            return logLevel < _standardErrorFromLevel ? SysConsole.Out : SysConsole.Error;
        }
    }
}
