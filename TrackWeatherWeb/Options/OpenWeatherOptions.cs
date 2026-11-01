namespace TrackWeatherWeb.Options
{
    public class OpenWeatherOptions
    {
        public const string SectionName = "OpenWeatherApi";

        public string BaseUrl { get; set; } = "https://api.openweathermap.org";
        public string ApiKey { get; set; } = string.Empty;
    }
}
