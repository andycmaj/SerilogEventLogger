using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace SerilogEventLogger
{
    public class EventData : Dictionary<string, object>
    {
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

            var pairs = values as IEnumerable<KeyValuePair<string, object>>;
            if (pairs != null)
            {
                // ASPNETCORE logger states and scopes are IReadOnlyList<KeyValuePair>
                foreach(var pair in pairs)
                {
                    this[pair.Key] = pair.Value;
                }
            }
            else if (!typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(values.GetType()))
            {
                // Flat object was passed in
                var props = TypeDescriptor.GetProperties(values);
                foreach (PropertyDescriptor prop in props)
                {
                    var val = prop.GetValue(values);
                    this[prop.Name]  = val;
                }
            }
        }

        internal IEnumerable<LogEventProperty> ToSerilogProperties(ILogEventPropertyFactory propertyFactory)
        {
            return this.Select(pair => propertyFactory.CreateProperty(pair.Key, pair.Value, true));
        }
    }
}
