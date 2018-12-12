# Event Logging

This library provides a different way to do logging in dotnet core applications.

> see [this github issue](https://github.com/aspnet/Logging/issues/533#issuecomment-433589765) for more context.

Templated message logs are great, but when you're piping your log events to an index like splunk, stackdriver, etc. you really don't need any message at all. When you're trying to consume application logs, the context/DATA is the most important thing. I don't care about an actual human-readable message, eg "A thing happened during {step}". In real-world scenarios where developers are looking at tons of log events, they want to be able to alert, graph, and data-mine. For these purposes, thousands of events with the same english sentence as a message have a very high noise-to-signal ratio mostly noise.

So when i think of structured event logging, I think of my log events as **events** consisting of *just the facts* `EventName`, `Data` (and other metadata like `Source`, `Level`, `StackTrace` etc.).

The problem is that 100% of existing dotnet logger APIs force you to log your events in the context of an interpolated string `message`.

This API provides an alternative that allows you to log RAW **events**.

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

> Log Scopes use `AsyncLocal` for storing the scope stack, and thus effect all logged events (inside the `IDisposable` scope) on the thread where `BeginScope` was called.

```csharp
// Start a logging Scope that will add {AccountId: 42, UserId: 99} to each Event
// logged inside its scope. When the 'ContentUploaded' Event is logged, its data will be
// {ContentLength: 2345, AccountId: 42, UserId: 99}
using (logger.BeginScope(new { AccountId = 42, UserId = 99 }))
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

## Example: Event logged in GCP Stackdriver

![stackdriver example](https://github.com/andycmaj/SerilogEventLogger/raw/master/docs/stackdriver%20event.png)

the `event.Data` in this example is collected from a combination of [`Scopes`](https://github.com/andycmaj/AspNetCore.ApplicationBlocks/blob/master/src/AspNetCore.ApplicationBlocks.FrontEnd/Middleware/RequestLoggingMiddleware.cs#L42) and [`Timers`](https://github.com/andycmaj/AspNetCore.ApplicationBlocks/blob/master/src/AspNetCore.ApplicationBlocks/Commands/LoggingCommandHandlerDecorator.cs#L28).

```csharp
// in RequestLoggingMiddleware
var requestData = new
{
    Connection = new
    {
        ...
        RemoteIpAddress = httpContext.Connection.RemoteIpAddress.ToString(),
    },
    ...
    RequestPath = httpContext.Request.Path.Value,
    RequestId = httpContext?.TraceIdentifier
};

using (_logger.BeginScope(requestData))
using (var timer = _logger.StartTimer("RequestTime"))
{
    await next();
    ...
```

```cshar
// in LoggingCommandHandlerDecorator
var data = new
{
    CommandType = typeof(TCommand).FullName,
    HandlerType = decoratee.GetType().FullName,
    IsAsync = isAsync
};

return Logger.StartTimer("ExecuteCommand", data);
```

The entire stack of context (`Scopes`, `Timers`, etc.) gets flattened and merged into each event that gets logged inside those scopes.

In Stackdriver, your event is inserted into and indexed as `jsonPayload.event`, and can be queried as such, eg. from the above event:

```
resource.labels.pod_id:"musigraph-prod-api-"
jsonPayload.event.Data.RequestId="0HLIU59J75NTJ:00000001"
```

## Logging Best Practices:

## event naming

your event names should be short `PascalCased` strings with no spaces.
they don't have to be namespaced or globally unique because the `LoggerName` event attribute will already be set to
the full Type name (`T`) of the injected `ILogger<T>`. for example:
`TheChunnel.Activities.Oce.Connector.Journeys.SynchronizeJourneys.SynchronizeJourneysCommand.HandlerAsync`...

example:

```csharp
// BAD
logger.WarningEvent("GetRecipientsCommand Execute NoValidRecipients");
```

```csharp
// GOOD
logger.WarningEvent("NoValidRecipients");
```

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

See [my `ApplicationBlocks.Logging` DI module](https://github.com/andycmaj/AspNetCore.ApplicationBlocks/blob/master/src/AspNetCore.ApplicationBlocks/Logging/LoggingModule.cs#L30) for an example of how to configure and register your `IEventLogger<>` instances.

### DotNetCore + Ninject Example

```csharp
            container.Register(typeof(IEventLogger<>), typeof(EventLogger<>), defaultLifestyle);

            container.RegisterSingleton(() =>
            {
                var serilogConfig = 
                    new LoggingConfiguration(
                        // From your app config or wherever
                        config.Environment,
                        config.Application,
                        config.Version,
                        config.Hostname
                    );

                Log.Logger = serilogConfig.CreateLogger();

                // EventLogger<> instances have a ctor dependency on serilog's ILogger as their underlying logger sink.
                return Log.Logger;
            });

```

