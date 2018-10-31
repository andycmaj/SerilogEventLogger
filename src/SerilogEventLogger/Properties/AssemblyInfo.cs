using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SerilogEventLogger.Kinesis")]
[assembly: InternalsVisibleTo("SerilogEventLogger.Tests")]
[assembly: InternalsVisibleTo("SerilogEventLogger.Kinesis.Tests")]
[assembly: InternalsVisibleTo("SerilogEventLogger.NetCoreLoggingExtensions")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Required to do AutoFixture magic on internal interfaces
