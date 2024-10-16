using BenchmarkDotNet.Running;
using LoggingExperiment;

#if !DEBUG
BenchmarkRunner.Run<WeatherServiceBenchmarks>();
return;
#endif

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WeatherService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast-improper-structuring", (WeatherService weatherService) =>
{
    return weatherService.GetForecastImproperStructuring();
});

app.MapGet("/weatherforecast-proper-structuring", (WeatherService weatherService) =>
{
    return weatherService.GetForecastProperStructuring();
});

app.Run();