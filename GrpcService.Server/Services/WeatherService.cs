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

        public override async Task<Empty> PrintStream(IAsyncStreamReader<PrintRequest> requestStream, ServerCallContext context)
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                _logger.LogInformation($"Client said: {request.Message}");
            }
            return new Empty();
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
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var temperatures = await GetCurrentTemperaturesAsync(request, httpClient);
                await responseStream.WriteAsync(new WeatherResponse
                {
                    Temperature = temperatures!.Main.Temp,
                    FeelsLike = temperatures.Main.FeelsLike,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    City = request.City,
                    Units = request.Units
                });
                await Task.Delay(1000);
            }
        }

        public override async Task<MultiWeatherResponse> GetMultiCurrentWeatherStream(IAsyncStreamReader<GetCurrentWeatherForCityRequest>
            requestStream, ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = new MultiWeatherResponse {Weather = { }};
            await foreach (var request in requestStream.ReadAllAsync())
            {
                var temperatures = await GetCurrentTemperaturesAsync(request, httpClient);
                response.Weather.Add(new WeatherResponse
                {
                    Temperature = temperatures!.Main.Temp,
                    FeelsLike = temperatures.Main.FeelsLike,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    City = request.City,
                    Units = request.Units
                });
            }
            return response;
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
