using TrackWeatherWeb.DTOs;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Repositories.Interfaces
{
    public interface ITransportRepo
    {
        Task<List<RequestTransportDTO>> GetTransportRequestsAsync();
        Task<RequestTransportDTO?> GetTransportRequestAsync(int id);
        Task<BaseResponse> RequestTransportAsync(string email, CreateTransportRequestDTO model);
        Task<BaseResponse> DeleteTransportRequestAsync(int id);
        Task<BaseResponse> UpdateTransportRequestAsync(int id, CreateTransportRequestDTO model);
    }
}
