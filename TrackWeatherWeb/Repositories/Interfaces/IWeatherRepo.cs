using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.Repositories.Interfaces
{
    public interface IWeatherRepo
    {
        Task<WeatherDTO> GetCurrentWeatherAsync(double lat, double lon, string language);
    }
}
