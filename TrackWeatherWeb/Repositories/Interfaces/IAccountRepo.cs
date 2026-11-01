using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.Repositories.Interfaces
{
    public interface IAccountRepo
    {
        Task<RegisterResponse> RegisterAsync(RegisterDTO model);
        Task<RegisterResponse> CreateUserAsync(CreateUserDTO model);
        Task<LoginResponse> LoginAsync(LoginDTO model);
        Task<GetUsersDTO?> AuthenticateAsync(LoginDTO model);
        Task<List<GetUsersDTO>> GetUsersAsync();
        Task<GetUsersDTO?> GetUserAsync(string email);
        Task<BaseResponse> DeleteUserAsync(string email, string? actingUserEmail);
        Task<RegisterResponse> UpdateUserAsync(string email, UpdateUserDTO model);
    }
}
