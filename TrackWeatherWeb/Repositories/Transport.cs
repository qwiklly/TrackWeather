using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Logs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace TrackWeatherWeb.Repositories
{
    public class Transport(AppDbContext appDbContext) : ITransport
    {
        // Retrieves all user coordinates.
        public async Task<IEnumerable<RequestTransportDTO>> GetTransportRequestsAsync()
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
                LogException.LogExceptions(ex);
                //display user exception
                throw new InvalidOperationException("Error while getting TransportRequests ");
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
                    return new BaseResponse(true, "Coordinates deleted successfully");
                }
                else
                {
                    return new BaseResponse(false, "Coordinates not found");
                }
            }
            catch (Exception ex)
            {
                LogException.LogExceptions(ex);

                return new BaseResponse(false, "Error while deleting TransportRequest ");
            }
        }

        // Updates a specific coordinate entry by its ID.
        public async Task<ActionResult<BaseResponse>> UpdateTransportRequestAsync(int id, RequestTransportDTO model)
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

                return new BaseResponse(true, "Coordinates updated successfully.");
            }
            catch (Exception ex)
            {
                LogException.LogExceptions(ex);

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
                return new BaseResponse(true, "Success");
            }
            catch (Exception ex)
            {
                LogException.LogExceptions(ex);
                return new BaseResponse(false, "Error while requesting TransportRequest ");
            }
        }
    }
}
