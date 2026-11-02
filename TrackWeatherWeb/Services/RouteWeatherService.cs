using Microsoft.Extensions.Localization;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories;
using TrackWeatherWeb.Repositories.Interfaces;
using TrackWeatherWeb.WeatherModels;

namespace TrackWeatherWeb.Services
{
    public interface IRouteWeatherService
    {
        Task<RouteWeatherDTO> AnalyzeAsync(RouteWeatherRequestDTO request, string language, CancellationToken cancellationToken);
    }

    public class RouteWeatherService(
        IWeatherRepo weatherRepo,
        IStringLocalizer<SharedResource> localizer,
        TimeProvider timeProvider,
        ILogger<RouteWeatherService> logger) : IRouteWeatherService
    {
        // A forecast entry describes three hours, so anything further away is not about this moment
        private const long MaxForecastDistanceSeconds = 3 * 60 * 60;
        private const int MaxConcurrentRequests = 4;

        public async Task<RouteWeatherDTO> AnalyzeAsync(RouteWeatherRequestDTO request, string language, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var departure = request.Departure?.ToUniversalTime() ?? now;
            if (departure < now)
            {
                departure = now;
            }

            var route = RouteSampler.Sample(request.Points);
            var forecasts = new IReadOnlyList<ForecastItem>?[route.Samples.Count];

            await Parallel.ForEachAsync(
                Enumerable.Range(0, route.Samples.Count),
                new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentRequests, CancellationToken = cancellationToken },
                async (index, _) => forecasts[index] = await TryGetForecastAsync(route.Samples[index], language));

            // One failed point should not hide the rest of the route, but a total outage is an error
            if (forecasts.All(f => f == null))
            {
                throw new WeatherUnavailableException("No forecast could be loaded for the route");
            }

            var points = route.Samples
                .Select((sample, index) => ToPoint(sample, departure.AddSeconds(sample.Seconds), forecasts[index]))
                .ToList();

            return new RouteWeatherDTO
            {
                OverallRisk = points.Max(p => p.Risk),
                DepartureUtc = departure,
                TotalKm = Math.Round(route.TotalKm, 1),
                Points = points
            };
        }

        private async Task<IReadOnlyList<ForecastItem>?> TryGetForecastAsync(RouteSample sample, string language)
        {
            try
            {
                return await weatherRepo.GetForecastAsync(sample.Lat, sample.Lon, language);
            }
            catch (Exception ex) when (ex is WeatherUnavailableException or HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
            {
                logger.LogWarning(ex, "No forecast for a route point");
                return null;
            }
        }

        private RouteWeatherPointDTO ToPoint(RouteSample sample, DateTime eta, IReadOnlyList<ForecastItem>? forecast)
        {
            var point = new RouteWeatherPointDTO
            {
                Lat = sample.Lat,
                Lon = sample.Lon,
                DistanceKm = Math.Round(sample.DistanceKm, 1),
                EtaUtc = DateTime.SpecifyKind(eta, DateTimeKind.Utc),
                StartIndex = sample.StartIndex,
                EndIndex = sample.EndIndex,
                Risk = RiskLevel.Unknown
            };

            var item = FindNearest(forecast, eta);
            if (item == null)
            {
                return point;
            }

            var assessment = RouteRiskEvaluator.Evaluate(item);
            point.Temp = item.Main?.Temp;
            point.Summary = item.Weather?.FirstOrDefault()?.Description;
            point.Risk = assessment.Level;
            point.Hazards = assessment.Hazards
                .Select(h => new RouteHazardDTO { Code = h.Hazard, Level = h.Level, Title = localizer[$"Hazard.{h.Hazard}"] })
                .ToList();
            return point;
        }

        private static ForecastItem? FindNearest(IReadOnlyList<ForecastItem>? forecast, DateTime eta)
        {
            if (forecast == null || forecast.Count == 0)
            {
                return null;
            }

            var etaUnix = new DateTimeOffset(eta, TimeSpan.Zero).ToUnixTimeSeconds();
            var nearest = forecast.MinBy(item => Math.Abs(item.Dt - etaUnix))!;
            return Math.Abs(nearest.Dt - etaUnix) <= MaxForecastDistanceSeconds ? nearest : null;
        }
    }
}
