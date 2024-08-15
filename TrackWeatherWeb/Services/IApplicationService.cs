using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.Services
{
    public interface IApplicationService
    {
        Task<BaseResponse> GetUsersAsync();
        Task<BaseResponse> GetCoordinatesAsync();
        Task<RegisterResponse> RegisterAsync(RegisterDTO model);
        Task<LoginResponse> LoginAsync(LoginDTO model);
        Task<BaseResponse> Confirm_pointAsync(RequestTransportDTO model);
        Task<BaseResponse> DeleteUserAsync(string email);
        Task<BaseResponse> DeleteCoordinatesAsync(int id);
        Task<BaseResponse> UpdateUserAsync(RegisterDTO model);
        Task<BaseResponse> UpdateCoordinatesAsync(RequestTransportDTO model);
    }
}

