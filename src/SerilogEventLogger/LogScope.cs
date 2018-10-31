using System;

#if NET451
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace SerilogEventLogger
{
    public class LogScope
    {
        private readonly string _name;
        public object State { get; }

        internal LogScope(string name, object state)
        {
            _name = name;
            State = state;
        }

        public LogScope Parent { get; private set; }

#if NET451
        private static readonly string FieldKey = $"{typeof(LogScope).FullName}.Value.{AppDomain.CurrentDomain.Id}";
        public static LogScope Current
        {
            get
            {
                var handle = CallContext.LogicalGetData(FieldKey) as ObjectHandle;
                if (handle == null)
                {
                    return default(LogScope);
                }

                return (LogScope)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData(FieldKey, new ObjectHandle(value));
            }
        }
#else
        private static AsyncLocal<LogScope> _value = new AsyncLocal<LogScope>();
        public static LogScope Current
        {
            set
            {
                _value.Value = value;
            }
            get
            {
                return _value.Value ?? default(LogScope);
            }
        }
#endif

        public static IDisposable Push(string name, object state)
        {
            var temp = Current;
            Current = new LogScope(name, state);
            Current.Parent = temp;

            return new DisposableScope();
        }

        public override string ToString()
        {
            return State?.ToString();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
                if (Current == null) {
                    return;
                }

                Current = Current.Parent;
            }
        }
    }
}
