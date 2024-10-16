using BenchmarkDotNet.Attributes;
using static LoggingExperiment.WeatherService;

namespace LoggingExperiment
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class WeatherServiceBenchmarks
    {
        private WeatherService _weatherService;

        [GlobalSetup]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<WeatherService>>();

            _weatherService = new WeatherService(logger);
        }

        [Benchmark(Baseline = true)]
        public WeatherForecast[] Interpolation()
        {
            return _weatherService.GetForecastImproperStructuring();
        }

        [Benchmark]
        public WeatherForecast[] Template()
        {
            return _weatherService.GetForecastProperStructuring();
        }
    }
}
