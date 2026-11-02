namespace TrackWeatherWeb.WeatherModels
{
    public class WeatherDescription
    {
        // Condition code, see https://openweathermap.org/weather-conditions
        public int Id { get; set; }
        public string? Main { get; set; }
        public string? Description { get; set; }
    }
}
