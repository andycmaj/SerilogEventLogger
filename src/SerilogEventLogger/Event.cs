using System;
using System.Collections.Generic;

namespace SerilogEventLogger
{
    public class Event
    {
        public string Name { get; }
        public EventData Data { get; }
        public Severity Severity { get; }
        public DateTime Stamp { get; }

        public Event(string name, object data, Severity severity = Severity.Information, DateTime? stamp = null)
        {
            Name = name;
            Data = new EventData(data);
            Severity = severity;
            Stamp = stamp ?? DateTime.UtcNow;
        }

        public Event(string name, EventData data, Severity severity = Severity.Information, DateTime? stamp = null)
        {
            Name = name;
            Data = data;
            Severity = severity;
            Stamp = stamp ?? DateTime.UtcNow;
        }

        public Event With(IDictionary<string, object> additionalData)
        {
            foreach (var pair in additionalData)
            {
                Data[pair.Key] = pair.Value;
            }

            return this;
        }

        public Event With(string key, object value)
        {
            Data[key] = value;

            return this;
        }
    }
}
