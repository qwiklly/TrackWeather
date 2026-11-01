using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Repositories.Interfaces;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Repositories
{
    public class TransportRepo(AppDbContext appDbContext, IStringLocalizer<SharedResource> localizer) : ITransportRepo
    {
        // Retrieves all transport requests, newest first.
        public Task<List<RequestTransportDTO>> GetTransportRequestsAsync() =>
            appDbContext.Requests
                .AsNoTracking()
                .OrderByDescending(r => r.Timestamp)
                .Select(r => new RequestTransportDTO
                {
                    Id = r.Id,
                    Email = r.Email,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    Comment = r.Comment,
                    Timestamp = r.Timestamp
                })
                .ToListAsync();

        // Retrieves a specific request by id
        public Task<RequestTransportDTO?> GetTransportRequestAsync(int id) =>
            appDbContext.Requests
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new RequestTransportDTO
                {
                    Id = r.Id,
                    Email = r.Email,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    Comment = r.Comment,
                    Timestamp = r.Timestamp
                })
                .FirstOrDefaultAsync();

        // Deletes transportRequest.
        public async Task<BaseResponse> DeleteTransportRequestAsync(int id)
        {
            var request = await appDbContext.Requests.FindAsync(id);
            if (request == null)
            {
                return new BaseResponse(false, localizer["Transport.NotFound"], ResponseError.NotFound);
            }

            appDbContext.Requests.Remove(request);
            await appDbContext.SaveChangesAsync();
            return new BaseResponse(true, localizer["Transport.Deleted"]);
        }

        // Updates a specific request by its ID.
        public async Task<BaseResponse> UpdateTransportRequestAsync(int id, CreateTransportRequestDTO model)
        {
            var request = await appDbContext.Requests.FindAsync(id);
            if (request == null)
            {
                return new BaseResponse(false, localizer["Transport.NotFound"], ResponseError.NotFound);
            }

            request.Latitude = model.Latitude!.Value;
            request.Longitude = model.Longitude!.Value;
            request.Comment = model.Comment;
            request.Timestamp = DateTime.UtcNow;

            await appDbContext.SaveChangesAsync();
            return new BaseResponse(true, localizer["Transport.Updated"]);
        }

        // Adds a request on behalf of the signed-in user.
        public async Task<BaseResponse> RequestTransportAsync(string email, CreateTransportRequestDTO model)
        {
            appDbContext.Requests.Add(new TransportRequests
            {
                Email = email,
                Latitude = model.Latitude!.Value,
                Longitude = model.Longitude!.Value,
                Comment = model.Comment,
                Timestamp = DateTime.UtcNow
            });

            await appDbContext.SaveChangesAsync();
            return new BaseResponse(true, localizer["Transport.Added"]);
        }
    }
}
