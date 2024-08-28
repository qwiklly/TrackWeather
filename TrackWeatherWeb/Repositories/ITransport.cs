using TrackWeatherWeb.DTOs;
using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.Repositories
{
    public interface ITransport
    {
        Task<List<RequestTransportDTO>> GetTransportRequestsAsync();
        Task<WeatherDTO> GetCurrentWeatherAsync(double lat, double lon);
        Task<TransportRequests?> GetTransportRequestAsync(int id);
        Task<BaseResponse> RequestTransportAsync(RequestTransportDTO model);
        Task<BaseResponse> DeleteTransportRequestAsync(DeleteTransportRequestDTO model);
        Task<BaseResponse> UpdateTransportRequestAsync(int id, RequestTransportDTO model);
    }
}
