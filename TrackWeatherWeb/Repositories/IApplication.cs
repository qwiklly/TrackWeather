using Microsoft.AspNetCore.Mvc;
using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.Repositories
{
    public interface IApplication
    {
        Task<RegisterResponse> RegisterAsync(RegisterDTO model);
        Task<LoginResponse> LoginAsync(LoginDTO model);
        Task<List<GetUsersDTO>> GetUsersAsync();
        Task<List<RequestTransportDTO>> GetCoordinatesAsync();
        Task<ApplicationUsers?> GetUserAsync(string email);
        Task<TransportRequests?> GetCoordinateAsync(int id);
        Task<BaseResponse> RequestTransportAsync(RequestTransportDTO model);
        Task<BaseResponse> DeleteUserAsync(DeleteUserDTO model);
        Task<BaseResponse> DeleteCoordinatesAsync(DeleteTransportRequestDTO model);
        Task<ActionResult<BaseResponse>> UpdateCoordinatesAsync(int id, RequestTransportDTO model);
        Task<ActionResult<RegisterResponse>> UpdateUserAsync(string email, RegisterDTO model);
    }
}
