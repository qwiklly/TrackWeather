using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Globalization;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Options;
using TrackWeatherWeb.Repositories.Interfaces;
using TrackWeatherWeb.WeatherModels;

namespace TrackWeatherWeb.Repositories
{
    public class WeatherRepo(HttpClient httpClient, IOptions<OpenWeatherOptions> options, IMemoryCache cache) : IWeatherRepo
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        // Get current weather. Results are cached per ~1 km cell to save the API quota.
        public async Task<WeatherDTO> GetCurrentWeatherAsync(double lat, double lon, string language)
        {
            var cacheKey = FormattableString.Invariant($"weather:{lat:F2}:{lon:F2}:{language}");
            if (cache.TryGetValue(cacheKey, out WeatherDTO? cached) && cached != null)
            {
                return cached;
            }

            // Invariant formatting: under the ru culture "51.67" would otherwise be sent as "51,67"
            var query = string.Create(CultureInfo.InvariantCulture,
                $"data/2.5/weather?lat={lat}&lon={lon}&units=metric&lang={language}&appid={options.Value.ApiKey}");

            using var response = await httpClient.GetAsync(query);
            if (!response.IsSuccessStatusCode)
            {
                // The request URI holds the API key, so it must stay out of the message
                throw new WeatherUnavailableException($"OpenWeather responded with {(int)response.StatusCode}");
            }

            var rawWeather = await response.Content.ReadFromJsonAsync<OpenWeatherResponse>()
                ?? throw new WeatherUnavailableException("OpenWeather returned an empty response");

            var weather = new WeatherDTO
            {
                Temp = rawWeather.Main?.Temp,
                Summary = string.Join(", ", rawWeather.Weather?.Select(x => x.Description) ?? []),
                City = rawWeather.Name
            };

            cache.Set(cacheKey, weather, CacheDuration);
            return weather;
        }
    }
}
