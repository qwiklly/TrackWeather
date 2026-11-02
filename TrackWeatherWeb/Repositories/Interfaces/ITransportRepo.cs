using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Services;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Repositories.Interfaces
{
    public interface ITransportRepo
    {
        Task<List<RequestTransportDTO>> GetTransportRequestsAsync(TransportStatus? status = null);
        Task<List<RequestTransportDTO>> GetRequestsForUserAsync(string email);
        Task<RequestTransportDTO?> GetTransportRequestAsync(int id);
        Task<BaseResponse> RequestTransportAsync(string email, CreateTransportRequestDTO model);
        Task<BaseResponse> DeleteTransportRequestAsync(int id);
        Task<BaseResponse> UpdateTransportRequestAsync(int id, CreateTransportRequestDTO model);
        Task<BaseResponse> AssignAsync(int id, string assigneeEmail, TransportActor actor);
        Task<BaseResponse> CompleteAsync(int id, TransportActor actor);
        Task<BaseResponse> CancelAsync(int id, TransportActor actor);
    }
}
