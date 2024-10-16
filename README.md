# CA2254 Logging Experiments
Practically investigating why "CA2254: Template should be a static expression" exists and what we can do about it.

![image](https://github.com/user-attachments/assets/fbbae513-08ed-476b-854f-1aa7c4ccc35a)


## What is it?

[CA2254: Template should be a static expression](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2254) is a warning that the log message doesn't have a consistent structure due to being interpolated or concatenated. 

The solution is to use a [message template](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#log-message-template-formatting).

If that isn't enough, the rest of this readme explains the benefits of using templates for logging.

## Structured logging

In short, giving structure to our logs helps us more easily read, parse, and query our logs.

[The NLog wiki](https://github.com/NLog/NLog/wiki/How-to-use-structured-logging):

> Structured logging makes it easier to store and query log-events, as the logevent message-template and message-parameters are preserved, instead of just transforming them into a formatted message.

To take an example from the [.NET logging documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#log-message-template), consider the following logger method:

```csharp
_logger.LogInformation("Getting item {Id} at {RunTime}", id, DateTime.Now);
```
For example, when logging to Azure Table Storage:

- Each Azure Table entity can have `ID` and `RunTime` properties.
- Tables with properties simplify queries on logged data. For example, a query can find all logs within a particular RunTime range without having to parse the time out of the text message.

Though, it may be the case that you're doing a small hobby project, or throwaway code, or anything that isn't going to have a massive amount of logs to trawl through when a production issue occurs, then you can probably ignore the warning and `#pragma` it away. However, there are also performance implications of using string interpolation regardless of intent of using full blown structured logging.

## Performance

The heading [Avoid string interpolation in logging](https://learn.microsoft.com/en-gb/dotnet/core/extensions/logging-library-authors#avoid-string-interpolation-in-logging) explains:
>String interpolation in logging is problematic for performance, as the string is evaluated even if the corresponding LogLevel isn't enabled. 

This is because the string interpolated value is evaluated at the time of passing the string into the `ILogger` call, not after the log message is ready to be sent to a provider. For example if we look at interpolation:

```csharp
var time = "3:00 PM";

// This interpolated string
_logger.LogInformation($"The time is currently: {time}");

// has the interpolation cost done to it before entering the LogInformation() method, meaning the call ends up being:
_logger.LogInformation($"The time is currently: 3:00 PM");
```

Contrast this with using a log message template:
```csharp
var time = "3:00 PM";

// This log template is passed as is to the underlying call without upfront interpolation cost
_logger.LogInformation("The time is currently: {currentTime}", time);
```

You can see for yourself in the `Logger` class in [Logger.cs](https://github.com/dotnet/runtime/blob/e1a14a8f284b94b31b84d62067773b2f9a5e2547/src/libraries/Microsoft.Extensions.Logging/src/Logger.cs) where we can pick up a couple of key checks before a log message is written:

1. [If there are any providers to log to](https://github.com/dotnet/runtime/blob/e1a14a8f284b94b31b84d62067773b2f9a5e2547/src/libraries/Microsoft.Extensions.Logging/src/Logger.cs#L30). E.g. Console, EventSource, EventLog, etc
2. [If the right level is enabled for this message for this provider](https://github.com/dotnet/runtime/blob/e1a14a8f284b94b31b84d62067773b2f9a5e2547/src/libraries/Microsoft.Extensions.Logging/src/Logger.cs#L39)

Only then is work put in to interpolate our string. 

### Metrics

Let's look at performance metrics. The benchmarks are off the back of the default weather example when creating a new minimal API with some of the randomness and allocations removed. Work is still left in to ensure our benchmarks aren't jitted away. There will be two benchmarks: _Interpolation_, and _Template_. They both use `LogInformation()` for `information` level logging.

```csharp
// Interpolation
_logger.LogInformation($"Returning weather forecast. Day 1 summary: {forecast[0].Summary}");

// Template
_logger.LogInformation("Returning weather forecast. Day 1 summary: {summary}", forecast[0].Summary);
```

 Both will log the same string: 
> "Returning weather forecast. Day 1 summary: Sweltering"

#### No provider

No log message is written regardless of level.

| Method        |     Mean | Ratio | Allocated | Alloc Ratio |
| ------------- | -------: | ----: | --------: | ----------: |
| Interpolation | 40.86 ns |  1.00 |     128 B |        1.00 |
| Template      | 44.19 ns |  1.08 |      32 B |        0.25 |

With no providers to log to, the interpolation occuring creates more allocations than the template - increasing garbage collection pressure. The template allocations are due to calling into a function with a `params` parameter, leading to an array allocation (I believe).

#### Console provider with warning log level 

The `Logger` object will attempt to log to the console, but due to the `warning` log level, none of the `information` logs will be written.

| Method        |     Mean | Ratio | Allocated | Alloc Ratio |
| ------------- | -------: | ----: | --------: | ----------: |
| Interpolation | 41.62 ns |  1.00 |     128 B |        1.00 |
| Template      | 45.75 ns |  1.10 |      32 B |        0.25 |

Same outcome as having no provider.

#### Console provider with information log level

The `Logger` object will log the weather string to the console.

| Method        |     Mean | Ratio | Allocated | Alloc Ratio |
| ------------- | -------: | ----: | --------: | ----------: |
| Interpolation | 235.9 us |  1.00 |     360 B |        1.00 |
| Template      | 230.2 us |  0.98 |     392 B |        1.09 |

When writing a log message, slightly more allocation has come from the template version. However this is acceptable as most logging calls in a production environment are skipped over. Unless needed, it's a poor idea to have logs in production to be at a lower, more verbose level. Meaning there is an overall gain in memory savings, and an overall reduction in GC pressure when templating.

## Notes
- Needed to remove the log levels from appsettings.json and appsettings.Development.json which override the logging levels
- The project is runnable and has two endpoints returning the same value:
	- https://localhost:7023/weatherforecast-improper-structuring
	- https://localhost:7023/weatherforecast-proper-structuring
- To run the benchmarks do `dotnet run --configuration Release`

## Links
- [CA2254: Template should be a static expression](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2254)
- [Logging in C# and .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [Logging in .NET Core and ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/)
= [Logging guidance for .NET library authors](https://learn.microsoft.com/en-gb/dotnet/core/extensions/logging-library-authors)
- [How to use structured logging by NLog](https://github.com/NLog/NLog/wiki/How-to-use-structured-logging)
- [ZLogger: A zero allocation text/structured logger](https://github.com/Cysharp/ZLogger) (for interest)
