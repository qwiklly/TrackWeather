using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Newtonsoft.Json;
using TrackWeatherWeb.WeatherModels;

namespace TrackWeatherWeb.Repositories
{
    public class Transport(AppDbContext appDbContext, IConfiguration _configuration, IHttpClientFactory _httpClientFactory) : ITransport
    {
        // Get current weather
        public async Task<WeatherDTO> GetCurrentWeatherAsync(double lat, double lon)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                client.BaseAddress = new Uri("https://api.openweathermap.org");
                string key = _configuration["OpenWeatherApi:ApiKey"]!;
                var response = await client.GetAsync($"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={key}&units=metric");
                response.EnsureSuccessStatusCode();

                var stringResult = await response.Content.ReadAsStringAsync();
                var rawWeather = JsonConvert.DeserializeObject<OpenWeatherResponse>(stringResult);
                return new WeatherDTO
                {
                    Temp = rawWeather.Main?.Temp,
                    Summary = string.Join(",", rawWeather.Weather?.Select(x => x.Main)!),
                    City = rawWeather.Name!
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting TransportRequests");
                throw;
            }
        }

        // Retrieves all user coordinates.
        public async Task<List<RequestTransportDTO>> GetTransportRequestsAsync()
        {
            try
            {
                return await appDbContext.Requests
                    .Select(u => new RequestTransportDTO
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Coordinate_x = u.Coordinate_x,
                        Coordinate_y = u.Coordinate_y,
                        Comment = u.Comment,
                        Timestamp = u.Timestamp
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting TransportRequests");
                //display user exception
                throw;
            }
        }

        // Retrieves a specific coordinate by id
        public async Task<TransportRequests?> GetTransportRequestAsync(int Id)
            => await appDbContext.Requests.FirstOrDefaultAsync(e => e.Id == Id);

        // Deletes transportRequest.
        public async Task<BaseResponse> DeleteTransportRequestAsync(DeleteTransportRequestDTO model)
        {
            try
            {
                var coordinates = await GetTransportRequestAsync(model.Id);
                if (coordinates != null)
                {
                    appDbContext.Requests.Remove(coordinates);
                    await appDbContext.SaveChangesAsync();
                    return new BaseResponse(true, "Transport request deleted successfully");
                }
                else
                {
                    return new BaseResponse(false, "Coordinates not found");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while deleting TransportRequest");

                return new BaseResponse(false, "Error while deleting TransportRequest ");
            }
        }

        // Updates a specific coordinate entry by its ID.
        public async Task<BaseResponse> UpdateTransportRequestAsync(int id, RequestTransportDTO model)
        {
            try
            {
                var coordinates = await GetTransportRequestAsync(id);

                if (coordinates == null)
                {
                    return new BaseResponse(false, $"Coordinates with ID '{id}' not found.");
                }

                coordinates.Coordinate_x = model.Coordinate_x;
                coordinates.Coordinate_y = model.Coordinate_y;
                coordinates.Comment = model.Comment ?? coordinates.Comment;
                coordinates.Timestamp = DateTime.UtcNow;

                await appDbContext.SaveChangesAsync();

                return new BaseResponse(true, "Transport request updated successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while updating TransportRequest");

                return new BaseResponse(false, "Error while updating TransportRequest ");
            }
        }

        //Adding coordinates
        public async Task<BaseResponse> RequestTransportAsync(RequestTransportDTO model)
        {
            try
            {
                appDbContext.Requests.Add(
                     new TransportRequests()
                     {
                         Email = model.Email,
                         Coordinate_x = model.Coordinate_x,
                         Coordinate_y = model.Coordinate_y,
                         Comment = model.Comment,
                         Timestamp = DateTime.UtcNow
                     });

                await appDbContext.SaveChangesAsync();
                return new BaseResponse(true, "Transport request added successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while requesting TransportRequest");
                return new BaseResponse(false, "Error while requesting TransportRequest ");
            }
        }
    }
}
