using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.WeatherModels;

namespace TrackWeatherWeb.Repositories.Interfaces
{
    public interface IWeatherRepo
    {
        Task<WeatherDTO> GetCurrentWeatherAsync(double lat, double lon, string language);
        Task<IReadOnlyList<ForecastItem>> GetForecastAsync(double lat, double lon, string language);
    }
}
