namespace GrpcService.Server
{
    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Contracts;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class WeatherService : Weather.WeatherBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<WeatherService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }
        public override async Task<WeatherResponse> GetCurrentWeather(GetCurrentWeatherForCityRequest request,
            ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var temperatures = await GetCurrentTemperaturesAsync(request, httpClient);
            return new WeatherResponse
            {
                Temperature = temperatures!.Main.Temp,
                FeelsLike = temperatures.Main.FeelsLike,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }

        public override async Task GetCurrentWeatherStream(GetCurrentWeatherForCityRequest request,
            IServerStreamWriter<WeatherResponse> responseStream,
            ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            for (int i = 0; i < 30; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Request was cancelled");
                    break;
                }
                var temperatures = await GetCurrentTemperaturesAsync(request, httpClient);
                await responseStream.WriteAsync(new WeatherResponse
                {
                    Temperature = temperatures!.Main.Temp,
                    FeelsLike = temperatures.Main.FeelsLike,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                });
                await Task.Delay(1000);
            }
        }


        private  async Task<Temperatures> GetCurrentTemperaturesAsync(GetCurrentWeatherForCityRequest request, HttpClient httpClient)
        {
            var responseText = await httpClient.GetStringAsync(
                $"http://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={_configuration.GetValue<string>("apiKey")}&units={request.Units}");
            var temperatures = JsonSerializer.Deserialize<Temperatures>(responseText);
            return temperatures;
        }
    }
}
