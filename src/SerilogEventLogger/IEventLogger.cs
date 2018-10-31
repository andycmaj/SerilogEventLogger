using System;

namespace SerilogEventLogger
{
    public interface IEventLogger
    {
        /// <summary>
        /// Log an Event without specifying Severity.
        /// For example, <c>logger.LogEvent("JobStarted", new { command.JobType, command.JobInput })</c>
        /// </summary>
        void LogEvent(string eventName, object data = null);

        /// <summary>
        /// Log an Event with a specified Severity
        /// For example, <c>logger.LogEvent("JobFailed", Severity.Alert, new { ErrorMessage = "job failed..."})</c>
        /// </summary>
        void LogEvent(string eventName, Severity severity, object data = null);

        /// <summary>
        /// Log an Exception event
        /// For example, <c>logger.LogEvent("JobFailed", ex, Severity.Error, new { command.JobInput })</c>
        /// </summary>
        void LogEvent(
            string eventName,
            Exception exception,
            Severity severity = Severity.Error,
            object data = null
        );

        /// <summary>
        /// Begins a logical operation scope. In the course of logging information within your
        /// application, you can group a set of logical operations within a scope. Any Events logged
        /// inside this scope will have the Scope's state properties applied to the Event's own
        /// data.
        /// For example, any Events logged in the scope of BeginScope(new { AppatureAccountId = 42})
        /// will have AppatureAccountId=42 added to their Data.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        IDisposable BeginScope(object state);

        /// <summary>
        /// A counter is a cumulative metric that represents a single numerical
        /// value that only ever goes up. A counter is typically used to count
        /// requests served, tasks completed, errors occurred, etc. Counters
        /// should not be used to expose current counts of items whose number
        /// can also go down, e.g. the number of currently running processes.
        /// Use gauges for that use case.
        /// </summary>
        /// <param name="counterName">EventName</param>
        /// <param name="count">Count to log for this Event</param>
        /// <param name="data">Additional data to be logged with the event</param>
        void Count(string counterName, int count = 1, object data = null);

        /// <summary>
        /// A gauge is a metric that represents a single numerical value that
        /// can arbitrarily go up and down. Gauges are typically used for
        /// measured values like temperatures or current memory usage, but also
        /// "counts" that can go up and down, like the number of running
        /// processes. Gauges created using MoveGauge would consume the data
        /// using a query like <c>| stats sum(Data.Delta)</c>
        /// </summary>
        /// <param name="gaugeName">EventName</param>
        /// <param name="delta">
        /// The amount by which the Gauge will be temporarily moved.
        /// IDisposable return value is disposed, the Gauge will be moved back by -1 * delta.
        /// </param>
        /// <param name="data">Additional data to be logged with the event</param>
        /// <returns>An IDisposable that moves the gauge back by -1 * delta when disposed</returns>
        IScopedMetric MoveGauge(string gaugeName, int delta = 1, object data = null);

        /// <summary>
        /// A gauge is a metric that represents a single numerical value that
        /// can arbitrarily go up and down. Gauges are typically used for
        /// measured values like temperatures or current memory usage, but also
        /// "counts" that can go up and down, like the number of running
        /// processes. Gauges created using SetGuage would consume the data in
        /// Splunk using a query like <c>| stats last(Data.Value)</c>
        /// </summary>
        /// <param name="gaugeName">EventName</param>
        /// <param name="value">
        /// The current Gauge value. </param>
        /// <param name="data">Additional data to be logged with the event</param>
        void SetGauge(string gaugeName, int value, object data = null);
        
        /// <summary>
        /// A timer is a stopwatch that can be used for profiling. The stopwatch starts when
        /// you call this method, and logs elapsed milliseconds when the IDisposable return value
        /// is disposed.
        /// </summary>
        /// <param name="timerName">EventName</param>
        /// <param name="data">Additional data to be logged with the event</param>
        /// <returns>
        /// An IDisposable that, when disposed, logs the elapsed time since StartTimer was called
        /// </returns>
        IScopedMetric StartTimer(string timerName, object data = null);
    }

    public interface IEventLogger<TLogger> : IEventLogger
    {
    }
}
