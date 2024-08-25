namespace TrackWeatherWeb.WeatherModels
{
    public class OpenWeatherResponse
    {
        public string? Name { get; set; } 

        public IEnumerable<WeatherDescription>? Weather { get; set; }

        public Main? Main { get; set; }
    }
}
