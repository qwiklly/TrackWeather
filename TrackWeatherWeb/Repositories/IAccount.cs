using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.Repositories
{
    public interface IAccount
    {
        Task<RegisterResponse> RegisterAsync(RegisterDTO model);
        Task<LoginResponse> LoginAsync(LoginDTO model);
        Task<List<GetUsersDTO>> GetUsersAsync();
        Task<ApplicationUsers?> GetUserAsync(string email); 
        Task<BaseResponse> DeleteUserAsync(DeleteUserDTO model);    
        Task<RegisterResponse> UpdateUserAsync(string email, RegisterDTO model);
    }
}
