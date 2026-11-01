namespace TrackWeatherWeb.Repositories
{
    public class WeatherUnavailableException(string message) : Exception(message);
}
