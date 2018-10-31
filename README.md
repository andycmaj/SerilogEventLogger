# Event Logging

> [API Docs](http://logging.apidocs.iqvia.io/api)

## Logging Events

```csharp
// Log an Event with name 'JobStarted' at Severity.Information with data:
// {JobType: command.JobType, JobInput: command.JobInput}
logger.InfoEvent("JobStarted", new { command.JobType, command.JobInput });
```

```csharp
// Log an Event with name 'JobStarted' at Severity.Debug with data:
// {JobType: command.JobType, JobInput: command.JobInput}
logger.DebugEvent("JobStarted", new { command.JobType, command.JobInput });
```

```csharp
// Log an Exception Event with name 'JobFailed' at Severity.Alert with data:
// {JobType: command.JobType, JobInput: command.JobInput}
logger.AlertEvent("JobFailed", ex, new { command.JobType, command.JobInput });
```

## Scopes

```csharp
// Start a logging Scope that will add {AppatureAccountId: 42, UserId: 99} to each Event
// logged inside its scope. When the 'ContentUploaded' Event is logged, its data will be
// {ContentLength: 2345, AppatureAccountId: 42, UserId: 99}
using (logger.BeginScope(new { AppatureAccountId = 42, UserId = 99}))
{
    //...
    logger.LogEvent("ContentUploaded", new { command.ContentLength });
    //..
}
```

## Logging Metrics

### Timers

```csharp
// Note that data properties can be added to a Timer before it's disposed.
using (var timer = logger.StartTimer("WorkerRuntime", new { BatchSize = batch.Length }))
{
    var resultCount = worker.DoWork(batch);
    timer.Add("ProcessedCount", resultCount);
}
```

### Gauges

```csharp
// One Event will be logged with data = {Delta: 1} when gauge is created, and another
// Event will be logged with data = {Delta: -1} when `gauge` is disposed.
// Note that data properties can be added to a Gauge before it's disposed.
using (var gauge = logger.MoveGauge("ActiveWorkers"))
{
    var result = worker.DoWork();
    gauge.Add("Result", result);
}
```

## Events in Splunk

![gauge-event](/uploads/4771e01d3b3df47da5504b5d797ab2d8/gauge-event.png)

```csharp
using (logger.BeginScope(new { service = "ActivityService" })
using (var gauge = logger.MoveGauge("ActiveActivityWorkers"))
{
    var result = worker.DoWork();
}
```

## Logging Best Practices:

## event naming

your event names should be short `PascalCased` strings with no spaces.
they don't have to be namespaced or globally unique because the `LoggerName` event attribute will already be set to
the full Type name (`T`) of the injected `ILogger<T>`. for example:
`TheChunnel.Activities.Oce.Connector.Journeys.SynchronizeJourneys.SynchronizeJourneysCommand.HandlerAsync`...

example:

[event logger code](https://gitlab.ims.io//channels/TheChunnel/blob/master/activities/TheChunnel.Activities.Marketing/Email/DivideValidContactBatches/DivideValidContactBatchesCommand.cs#L93):

```csharp
// BAD
logger.WarningEvent("Chunnel GetRecipientsCommand Execute NoValidRecipients");
```

```csharp
// GOOD
logger.WarningEvent("NoValidRecipients");
```

![ScreenCapture_at_Wed_Oct_18_12_43_29_PDT_2017](/uploads/2f052a96a703bcfe54dc3f3ffe0be46b/ScreenCapture_at_Wed_Oct_18_12_43_29_PDT_2017.png)

## event data

be careful when you're logging external class instances as event data.
This can fail if there are circular references in the structure, and you also might accidentally end up logging sensitive information.

be explicit about what you want to log as `data`. use an anonymous object to explicitly whitelist what you want to log, for example:


```csharp
// BAD
var result = client.Call(...);
logger.ErrorEvent("ClientError", result);
```

```csharp
// GOOD
var result = client.Call(...);
logger.ErrorEvent("ClientError", new { ResultStatus = result.Status, Message = result.ErrorMessage });
```

## Configuring EventLogger

The `EventLogger` can be configured to write events to Kinesis and/or directly to a Splunk HTTPCollector endpoint.

* To write directly to a Splunk instance (used for logging locally on a dev box), you will need the local Splunk instance's HTTPCollector endpoint and a Token.

* To write to a Kinesis Stream (the preferred way of writing events in production environments), you'll need AWS credentials for the and the name of the Kinesis Stream.

### DotNetCore + Ninject Example

```csharp
// LoggingConfiguration contains application-scoped event context, such as `Environment`, `Version`, etc.
var loggingConfiguration =
    new LoggingConfiguration(
        config.Environment,
        config.ServiceName,
        config.Version,
        config.ServiceInstanceName
    ) { ShouldLogToConsole = false };
Bind<LoggingConfiguration>().ToConstant(loggingConfiguration);

// KinesisConfiguration contains values needed for writing to a Kinesis stream.
var kinesisConfig = new KinesisConfiguration();
loggingConfigurationSection.GetSection("Kinesis").Bind(kinesisConfig);

// SplunkConfiguration contains values needed for writing events directly to Splunk.
// This is the preferred way of writing to local dev-box Splunk instances.
var splunkConfig = new SplunkConfiguration();
loggingConfigurationSection.GetSection("Splunk").Bind(splunkConfig);

// ConfigureSinks gets you a Serilog logger configuration from which you can create a Serilog Logger, the underlying logger used by EventLoggers.
var serilogConfig = loggingConfiguration.ConfigureSinks(kinesisConfig, splunkConfig);
Log.Logger = serilogConfig.CreateLogger();

// Configuring IEventLogger<> to be injected
Bind<Serilog.ILogger>().ToConstant(Log.Logger);
Bind(typeof(IEventLogger<>)).To(typeof(EventLogger<>));
```

### Monolith Example

```csharp
private static readonly Lazy<KinesisConfiguration> kinesisConfig =
    new Lazy<KinesisConfiguration>(() =>
        new KinesisConfiguration(
            GetString("Logging.Kinesis.AwsAccessKeyId"),
            GetString("Logging.Kinesis.AwsSecretAccessKey"),
            GetString("Logging.Kinesis.AwsRegion"),
            GetString("Logging.Kinesis.StreamName")
        )
    );

private static readonly Lazy<SplunkConfiguration> splunkConfig =
    new Lazy<SplunkConfiguration>(() =>
        new SplunkConfiguration(
            GetString("Logging.Splunk.EventCollectorUrl"),
            GetString("Logging.Splunk.Token")
        )
    );

private static readonly Lazy<LoggingConfiguration> loggingConfig =
    new Lazy<LoggingConfiguration>(() => new LoggingConfiguration(
        GetString("Environment", "not-set"),
        AppDomain.CurrentDomain.FriendlyName,
        "unknown",
        Environment.MachineName
    )
);

public LoggerConfiguration LoggerConfiguration {
    get { return loggingConfig.Value.ConfigureSinks(kinesisConfig.Value, splunkConfig.Value); }
}

public bool IsSplunkLoggingEnabled {
    get { return kinesisConfig.Value.IsEnabled || splunkConfig.Value.IsEnabled; }
}
```
