using System;
using System.Collections.Generic;

namespace SerilogEventLogger
{
    public class ScopedMetric : IScopedMetric
    {
        private readonly Action<IDictionary<string, object>> onDisposed;
        private IDictionary<string, object> Data { get; }

        public ScopedMetric(
            Action<IDictionary<string, object>> onDisposed = null
        )
        {
            this.onDisposed = onDisposed;
            Data = new Dictionary<string, object>();
        }

        public void Add(string key, object value)
        {
            Data.Add(key, value);
        }

        public void Dispose()
        {
            if (onDisposed == null)
            {
                return;
            }

            onDisposed(Data);
        }
    }
}
