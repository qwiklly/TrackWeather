using System.Text.Json.Serialization;

namespace TrackWeatherWeb.WeatherModels
{
    // The 5 day / 3 hour forecast of OpenWeather
    public class ForecastResponse
    {
        public List<ForecastItem>? List { get; set; }
    }

    public class ForecastItem
    {
        // Unix time, seconds
        public long Dt { get; set; }
        public ForecastMain? Main { get; set; }
        public List<WeatherDescription>? Weather { get; set; }
        public ForecastWind? Wind { get; set; }
        // Metres, at most 10000
        public int? Visibility { get; set; }
        // Probability of precipitation, 0..1
        public double? Pop { get; set; }
        public ForecastPrecipitation? Rain { get; set; }
        public ForecastPrecipitation? Snow { get; set; }
    }

    public class ForecastMain
    {
        public double? Temp { get; set; }
        public int? Humidity { get; set; }
    }

    public class ForecastWind
    {
        public double? Speed { get; set; }
        public double? Gust { get; set; }
    }

    public class ForecastPrecipitation
    {
        [JsonPropertyName("3h")]
        public double? ThreeHours { get; set; }
    }
}
