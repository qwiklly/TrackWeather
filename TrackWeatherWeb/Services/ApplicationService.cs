using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.Services
{
    public class ApplicationService(HttpClient httpClient) : IApplicationService
    {
        private readonly HttpClient httpClient = httpClient;

        public async Task<BaseResponse> GetUsersAsync()
        {
            var response = await httpClient.GetAsync("api/account/getUsers");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}"); //request not completed
        }

        public async Task<BaseResponse> GetUserAsync(string email)
        {
            var response = await httpClient.PostAsJsonAsync("api/account/GetUser", email);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }

        public async Task<BaseResponse> GetCoordinatesAsync()
        {
            var response = await httpClient.GetAsync("api/account/getCoordinates");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }

        public async Task<BaseResponse> GetCoordinateAsync(int id)
        {
            var response = await httpClient.PostAsJsonAsync("api/account/GetCoordinate", id);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }

        public async Task<LoginResponse> LoginAsync(LoginDTO model)
        {
            var response = await httpClient.PostAsJsonAsync("api/account/login", model);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }

        public async Task<BaseResponse> DeleteUserAsync(string email)
        {
            var response = await httpClient.DeleteAsync($"api/account/delete/{email}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }

        public async Task<BaseResponse> DeleteCoordinatesAsync(int id)
        {
            var response = await httpClient.DeleteAsync($"api/deleteCoordinates/{id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }

        public async Task<BaseResponse> UpdateCoordinatesAsync(RequestTransportDTO model)
        {
            var response = await httpClient.PutAsJsonAsync("api/updateRequestTransport", model);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }

        public async Task<BaseResponse> UpdateUserAsync(RegisterDTO model)
        {
            var response = await httpClient.PutAsJsonAsync("api/account/updateRegister", model);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }

        public async Task<BaseResponse> Confirm_pointAsync(RequestTransportDTO model)
        {
            var response = await httpClient.PostAsJsonAsync("api/requestTransport", model);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BaseResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }
        public async Task<RegisterResponse> RegisterAsync(RegisterDTO model)
        {
            var response = await httpClient.PostAsJsonAsync("api/account/register", model);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
                return result!;
            }
            else
                throw new HttpRequestException($"Запрос не удался: {response.ReasonPhrase}");
        }
    }
}

