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
        private static readonly TimeSpan ForecastCacheDuration = TimeSpan.FromMinutes(30);

        // Get current weather. Results are cached per ~1 km cell to save the API quota.
        public async Task<WeatherDTO> GetCurrentWeatherAsync(double lat, double lon, string language)
        {
            var cacheKey = FormattableString.Invariant($"weather:{lat:F2}:{lon:F2}:{language}");
            if (cache.TryGetValue(cacheKey, out WeatherDTO? cached) && cached != null)
            {
                return cached;
            }

            var rawWeather = await GetAsync<OpenWeatherResponse>("weather", lat, lon, language);

            var weather = new WeatherDTO
            {
                Temp = rawWeather.Main?.Temp,
                Summary = string.Join(", ", rawWeather.Weather?.Select(x => x.Description) ?? []),
                City = rawWeather.Name
            };

            cache.Set(cacheKey, weather, CacheDuration);
            return weather;
        }

        // Get the 5 day forecast. Routes share cells of ~10 km, so similar routes reuse the same answer.
        public async Task<IReadOnlyList<ForecastItem>> GetForecastAsync(double lat, double lon, string language)
        {
            lat = Math.Round(lat, 1);
            lon = Math.Round(lon, 1);

            var cacheKey = FormattableString.Invariant($"forecast:{lat:F1}:{lon:F1}:{language}");
            if (cache.TryGetValue(cacheKey, out IReadOnlyList<ForecastItem>? cached) && cached != null)
            {
                return cached;
            }

            var response = await GetAsync<ForecastResponse>("forecast", lat, lon, language);
            IReadOnlyList<ForecastItem> forecast = response.List ?? [];

            cache.Set(cacheKey, forecast, ForecastCacheDuration);
            return forecast;
        }

        private async Task<T> GetAsync<T>(string endpoint, double lat, double lon, string language)
        {
            // Invariant formatting: under the ru culture "51.67" would otherwise be sent as "51,67"
            var query = string.Create(CultureInfo.InvariantCulture,
                $"data/2.5/{endpoint}?lat={lat}&lon={lon}&units=metric&lang={language}&appid={options.Value.ApiKey}");

            using var response = await httpClient.GetAsync(query);
            if (!response.IsSuccessStatusCode)
            {
                // The request URI holds the API key, so it must stay out of the message
                throw new WeatherUnavailableException($"OpenWeather responded with {(int)response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<T>()
                ?? throw new WeatherUnavailableException("OpenWeather returned an empty response");
        }
    }
}
