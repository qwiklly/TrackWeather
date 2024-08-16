using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.HttpServices
{
    public class ApplicationService : IApplicationService
    {
        private readonly HttpClient _httpClient;

        public ApplicationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private static async Task<T> SendRequestAsync<T>(Func<Task<HttpResponseMessage>> httpRequest)
        {
            var response = await httpRequest();
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>() ?? throw new HttpRequestException("Empty response received");
            }
            else
            {
                throw new HttpRequestException($"Request failed with status code {response.StatusCode} and reason {response.ReasonPhrase}.");
            }
        }

        public Task<BaseResponse> GetUsersAsync() =>
            SendRequestAsync<BaseResponse>(() => _httpClient.GetAsync("api/account/getUsers"));

        public Task<BaseResponse> GetUserAsync(string email) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.PostAsJsonAsync("api/account/GetUser", email));

        public Task<BaseResponse> GetCoordinatesAsync() =>
            SendRequestAsync<BaseResponse>(() => _httpClient.GetAsync("api/account/getCoordinates"));

        public Task<BaseResponse> GetCoordinateAsync(int id) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.PostAsJsonAsync("api/account/GetCoordinate", id));

        public Task<LoginResponse> LoginAsync(LoginDTO model) =>
            SendRequestAsync<LoginResponse>(() => _httpClient.PostAsJsonAsync("api/account/login", model));

        public Task<BaseResponse> DeleteUserAsync(string email) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.DeleteAsync($"api/account/delete/{email}"));

        public Task<BaseResponse> DeleteCoordinatesAsync(int id) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.DeleteAsync($"api/deleteCoordinates/{id}"));

        public Task<BaseResponse> UpdateCoordinatesAsync(RequestTransportDTO model) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.PutAsJsonAsync("api/updateRequestTransport", model));

        public Task<BaseResponse> UpdateUserAsync(RegisterDTO model) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.PutAsJsonAsync("api/account/updateRegister", model));

        public Task<BaseResponse> ConfirmTransportRequestAsync(RequestTransportDTO model) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.PostAsJsonAsync("api/requestTransport", model));

        public Task<RegisterResponse> RegisterAsync(RegisterDTO model) =>
            SendRequestAsync<RegisterResponse>(() => _httpClient.PostAsJsonAsync("api/account/register", model));
    }
}


