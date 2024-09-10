using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.HttpServices
{
    public class ApplicationService(HttpClient httpClient) : IApplicationService
    {
        private readonly HttpClient _httpClient = httpClient;

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
            SendRequestAsync<BaseResponse>(() => _httpClient.GetAsync("api/application/getUsers"));
        public Task<BaseResponse> GetWeatherAsync() =>
           SendRequestAsync<BaseResponse>(() => _httpClient.GetAsync("api/application/getWeather"));

        public Task<BaseResponse> GetUserAsync(string email) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.GetAsync($"api/application/getUser?email={email}"));

        public Task<BaseResponse> GetAllTransportRequestsAsync() =>
            SendRequestAsync<BaseResponse>(() => _httpClient.GetAsync("api/application/getAllTransportRequests"));

        public Task<BaseResponse> GetTransportRequestAsync(int id) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.GetAsync($"api/application/getTransportRequest?id={id}"));

        public Task<LoginResponse> LoginAsync(LoginDTO model) =>
            SendRequestAsync<LoginResponse>(() => _httpClient.PostAsJsonAsync("api/application/login", model));

        public Task<BaseResponse> DeleteUserAsync(string email) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.DeleteAsync($"api/application/deleteUser/{email}"));

        public Task<BaseResponse> DeleteTransportRequestAsync(int id) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.DeleteAsync($"api/application/deleteTransportRequests/{id}"));

        public Task<BaseResponse> UpdateTransportRequestAsync(RequestTransportDTO model, int id) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.PutAsJsonAsync("api/application/updateTransportRequests", model));

        public Task<BaseResponse> UpdateUserAsync(RegisterDTO model, string email) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.PutAsJsonAsync("api/application/updateUser", model));

        public Task<BaseResponse> ConfirmTransportRequestAsync(RequestTransportDTO model) =>
            SendRequestAsync<BaseResponse>(() => _httpClient.PostAsJsonAsync("api/application/requestTransport", model));

        public Task<RegisterResponse> RegisterAsync(RegisterDTO model) =>
            SendRequestAsync<RegisterResponse>(() => _httpClient.PostAsJsonAsync("api/application/register", model));
    }
}

