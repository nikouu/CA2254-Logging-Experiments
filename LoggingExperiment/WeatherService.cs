namespace LoggingExperiment
{
    public class WeatherService
    {
        private readonly ILogger<WeatherService> _logger;

        private readonly static WeatherForecast[] _forecasts = Enumerable.Range(1, 5).Select(index =>
               new WeatherForecast
               (
                   DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                   40,
                   "Sweltering"
               )).ToArray();

        public WeatherService(ILogger<WeatherService> logger)
        {
            _logger = logger;
        }

        public WeatherForecast[] GetForecastImproperStructuring()
        {
            var forecast = _forecasts;
            _logger.LogInformation($"Returning weather forecast. Day 1 summary: {forecast[0].Summary}");
            return forecast;
        }

        public WeatherForecast[] GetForecastProperStructuring()
        {
            var forecast = _forecasts;
            _logger.LogInformation("Returning weather forecast. Day 1 summary: {summary}", forecast[0].Summary);
            return forecast;
        }

        public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
        {
            public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        }
    }
}
