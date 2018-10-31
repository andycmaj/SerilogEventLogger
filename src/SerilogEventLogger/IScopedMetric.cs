using System;

namespace SerilogEventLogger
{
    public interface IScopedMetric : IDisposable
    {
        void Add(string key, object value);
    }
}
