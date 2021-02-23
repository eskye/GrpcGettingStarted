namespace GrpcService.Server
{
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Contracts;
    using Grpc.Core;
    using Microsoft.Extensions.Configuration;

    public class WeatherService : Weather.WeatherBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public WeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        public override async Task<WeatherResponse> GetCurrentWeather(GetCurrentWeatherForCityRequest request,
            ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var responseText = await httpClient.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={_configuration.GetValue<string>("apiKey")}&units={request.Units}");
            var temperatures = JsonSerializer.Deserialize<Temperatures>(responseText);
            return new WeatherResponse {Temperature = temperatures!.Main.Temp, FeelsLike = temperatures.Main.FeelsLike};
        }
    }
}
