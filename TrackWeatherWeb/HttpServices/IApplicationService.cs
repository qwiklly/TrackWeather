using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.HttpServices
{
    public interface IApplicationService
    {
        Task<BaseResponse> GetUsersAsync();
        Task<BaseResponse> GetWeatherAsync();
        Task<BaseResponse> GetAllTransportRequestsAsync();
        Task<RegisterResponse> RegisterAsync(RegisterDTO model);
        Task<LoginResponse> LoginAsync(LoginDTO model);
        Task<BaseResponse> ConfirmTransportRequestAsync(RequestTransportDTO model);
        Task<BaseResponse> DeleteUserAsync(string email);
        Task<BaseResponse> DeleteTransportRequestAsync(int id);
        Task<BaseResponse> UpdateUserAsync(RegisterDTO model, string email);
        Task<BaseResponse> UpdateTransportRequestAsync(RequestTransportDTO model, int id);
    }
}

