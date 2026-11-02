using System.Text.Json.Serialization;
using TrackWeatherWeb.WeatherModels;

namespace TrackWeatherWeb.Services
{
    [JsonConverter(typeof(JsonStringEnumConverter<RiskLevel>))]
    public enum RiskLevel
    {
        // No forecast for this place and time
        Unknown,
        Low,
        Moderate,
        High
    }

    [JsonConverter(typeof(JsonStringEnumConverter<Hazard>))]
    public enum Hazard
    {
        Ice,
        Snow,
        HeavyRain,
        Wind,
        PoorVisibility,
        Thunderstorm
    }

    public sealed record HazardFinding(Hazard Hazard, RiskLevel Level);

    public sealed record RiskAssessment(RiskLevel Level, IReadOnlyList<HazardFinding> Hazards);

    // Turns one forecast entry into a driving risk. The thresholds are practical estimates, not an official road safety standard.
    public static class RouteRiskEvaluator
    {
        // Below this probability a forecast shower is not treated as a hazard
        private const double MinPrecipitationProbability = 0.3;

        public static RiskAssessment Evaluate(ForecastItem item)
        {
            var findings = new List<HazardFinding>();
            var conditionId = item.Weather?.FirstOrDefault()?.Id ?? 0;
            var temp = item.Main?.Temp;
            var humidity = item.Main?.Humidity ?? 0;
            var rain = item.Rain?.ThreeHours ?? 0;
            var snow = item.Snow?.ThreeHours ?? 0;
            var isLikely = (item.Pop ?? 1) >= MinPrecipitationProbability;

            // Thunderstorm, 2xx
            if (conditionId is >= 200 and < 300)
            {
                findings.Add(new(Hazard.Thunderstorm, RiskLevel.High));
            }

            // Ice: freezing rain, liquid precipitation on a frozen road, or dampness around zero
            var isLiquid = rain > 0 || conditionId is >= 300 and < 600;
            if (conditionId == 511 || (isLikely && isLiquid && temp <= 1))
            {
                findings.Add(new(Hazard.Ice, RiskLevel.High));
            }
            else if (temp is >= -2 and <= 2 && humidity >= 90)
            {
                findings.Add(new(Hazard.Ice, RiskLevel.Moderate));
            }

            if (isLikely && snow >= 3)
            {
                findings.Add(new(Hazard.Snow, RiskLevel.High));
            }
            else if (isLikely && snow >= 0.2)
            {
                findings.Add(new(Hazard.Snow, RiskLevel.Moderate));
            }

            if (isLikely && rain >= 15)
            {
                findings.Add(new(Hazard.HeavyRain, RiskLevel.High));
            }
            else if (isLikely && rain >= 5)
            {
                findings.Add(new(Hazard.HeavyRain, RiskLevel.Moderate));
            }

            var speed = item.Wind?.Speed ?? 0;
            var gust = item.Wind?.Gust ?? 0;
            if (speed >= 15 || gust >= 25)
            {
                findings.Add(new(Hazard.Wind, RiskLevel.High));
            }
            else if (speed >= 10 || gust >= 17)
            {
                findings.Add(new(Hazard.Wind, RiskLevel.Moderate));
            }

            if (item.Visibility < 200)
            {
                findings.Add(new(Hazard.PoorVisibility, RiskLevel.High));
            }
            else if (item.Visibility < 1000)
            {
                findings.Add(new(Hazard.PoorVisibility, RiskLevel.Moderate));
            }

            var level = findings.Count == 0 ? RiskLevel.Low : findings.Max(f => f.Level);
            return new RiskAssessment(level, findings);
        }
    }
}
