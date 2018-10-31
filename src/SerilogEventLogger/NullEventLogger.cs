using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SerilogEventLogger
{
    public class NullEventLogger<T> : NullEventLogger, IEventLogger<T> { }

    public class NullEventLogger : IEventLogger
    {
        public IList<Event> LoggedEvents = new List<Event>();

        public IDisposable BeginScope(object state)
        {
            Console.WriteLine(JsonConvert.SerializeObject(state));
            return new ScopedMetric(_ => { });
        }

        public void Count(string counterName, int count = 1, object data = null)
        {
            Console.WriteLine($"Counter: {counterName} ({count}) | {JsonConvert.SerializeObject(data)}");
            LoggedEvents.Add(new Event(counterName, data, Severity.Information));
        }

        public void LogEvent(string eventName, object data = null)
        {
            Console.WriteLine($"Event: Information: {eventName} | {JsonConvert.SerializeObject(data)}");
            LoggedEvents.Add(new Event(eventName, data, Severity.Information));
        }

        public void LogEvent(string eventName, Severity severity, object data = null)
        {
            Console.WriteLine($"Event: {severity}: {eventName} | {JsonConvert.SerializeObject(data)}");
            LoggedEvents.Add(new Event(eventName, data, severity));
        }

        public void LogEvent(string eventName, Exception exception, Severity severity = Severity.Error, object data = null)
        {
            Console.WriteLine($"Event: {severity}: {eventName} | {JsonConvert.SerializeObject(data)}");
            Console.WriteLine(exception);
            LoggedEvents.Add(new Event(eventName, data, severity));
        }

        public IScopedMetric MoveGauge(string gaugeName, int delta = 1, object data = null)
        {
            Console.WriteLine($"Gauge: {gaugeName} (+{delta}) | {JsonConvert.SerializeObject(data)}");
            LoggedEvents.Add(new Event($"{gaugeName}+{delta}", data));
            return new ScopedMetric(_ =>
            {
                Console.WriteLine($"Gauge: {gaugeName} (-{delta}) | {JsonConvert.SerializeObject(data)}");
                LoggedEvents.Add(new Event($"{gaugeName}-{delta}", data));
            });
        }
        
        public void SetGauge(string gaugeName, int value, object data = null)
        {
            Console.WriteLine($"Gauge: {gaugeName} ({value}) | {JsonConvert.SerializeObject(data)}");
            LoggedEvents.Add(new Event($"{gaugeName}:{value}", data));
        }

        public IScopedMetric StartTimer(string timerName, object data = null)
        {
            Console.WriteLine($"TimerStarted: {timerName} | {JsonConvert.SerializeObject(data)}");
            LoggedEvents.Add(new Event($"{timerName}_started", data));
            return new ScopedMetric(_ =>
            {
                Console.WriteLine($"TimerStopped: {timerName} | {JsonConvert.SerializeObject(data)}");
                LoggedEvents.Add(new Event($"{timerName}_stopped", data));
            });
        }
    }
}
