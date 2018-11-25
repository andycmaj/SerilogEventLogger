using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Serilog.Events;
using Structurizer;

namespace SerilogEventLogger
{
    public class EventData : Dictionary<string, object>
    {
        private static readonly FlexibleStructureBuilder structurizer
            = new FlexibleStructureBuilder();

        public EventData() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public EventData(object values) : this()
        {
            AddValues(values);
        }

        public EventData(IDictionary<string, object> dictionary)
            : base(dictionary, StringComparer.OrdinalIgnoreCase)
        {
        }

        internal void AddValues(object values)
        {
            if (values == null)
            {
                return;
            }

            if (values is IEnumerable<KeyValuePair<string, object>> pairs)
            {
                // ASPNETCORE logger states and scopes are IReadOnlyList<KeyValuePair>
                foreach(var pair in pairs)
                {
                    this[pair.Key] = pair.Value;
                }
            }
            else if (!typeof(IEnumerable).IsAssignableFrom(values.GetType()))
            {
                // Flat object was passed in
                var type = values.GetType();
                var props = structurizer.CreateStructure(values);
                foreach (var index in props.Indexes)
                {
                    this[index.Path] = index.Value;
                }
            }
        }

        internal IEnumerable<LogEventProperty> ToSerilogProperties()
        {
            return this.Select(pair => new LogEventProperty(pair.Key, new ScalarValue(pair.Value)));
        }
    }
}
