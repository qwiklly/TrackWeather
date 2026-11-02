using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq.Expressions;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Repositories.Interfaces;
using TrackWeatherWeb.Services;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Repositories
{
    public class TransportRepo(
        AppDbContext appDbContext,
        IStringLocalizer<SharedResource> localizer,
        ITransportNotifier notifier,
        TimeProvider timeProvider) : ITransportRepo
    {
        private static readonly Expression<Func<TransportRequests, RequestTransportDTO>> ToDto = r => new RequestTransportDTO
        {
            Id = r.Id,
            Email = r.Email,
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            Comment = r.Comment,
            Timestamp = r.Timestamp,
            Status = r.Status,
            AssignedToEmail = r.AssignedToEmail,
            UpdatedAt = r.UpdatedAt
        };

        // Retrieves transport requests, newest first, optionally only those with one status.
        public Task<List<RequestTransportDTO>> GetTransportRequestsAsync(TransportStatus? status = null) =>
            appDbContext.Requests
                .AsNoTracking()
                .Where(r => status == null || r.Status == status)
                .OrderByDescending(r => r.Timestamp)
                .Select(ToDto)
                .ToListAsync();

        // Retrieves the requests a user created or was assigned to.
        public Task<List<RequestTransportDTO>> GetRequestsForUserAsync(string email) =>
            appDbContext.Requests
                .AsNoTracking()
                .Where(r => r.Email == email || r.AssignedToEmail == email)
                .OrderByDescending(r => r.Timestamp)
                .Select(ToDto)
                .ToListAsync();

        // Retrieves a specific request by id
        public Task<RequestTransportDTO?> GetTransportRequestAsync(int id) =>
            appDbContext.Requests
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(ToDto)
                .FirstOrDefaultAsync();

        // Deletes transportRequest.
        public async Task<BaseResponse> DeleteTransportRequestAsync(int id)
        {
            var request = await appDbContext.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            appDbContext.Requests.Remove(request);
            await appDbContext.SaveChangesAsync();
            await notifier.NotifyAsync(new TransportChange(id, request.Status, TransportChangeKind.Deleted), [request.Email, request.AssignedToEmail]);
            return new BaseResponse(true, localizer["Transport.Deleted"]);
        }

        // Updates the place and comment of a request. Its creation time stays as it was.
        public async Task<BaseResponse> UpdateTransportRequestAsync(int id, CreateTransportRequestDTO model)
        {
            var request = await appDbContext.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            request.Latitude = model.Latitude!.Value;
            request.Longitude = model.Longitude!.Value;
            request.Comment = model.Comment;
            return await SaveChangeAsync(request, "Transport.Updated");
        }

        // Adds a request on behalf of the signed-in user.
        public async Task<BaseResponse> RequestTransportAsync(string email, CreateTransportRequestDTO model)
        {
            var request = new TransportRequests
            {
                Email = email,
                Latitude = model.Latitude!.Value,
                Longitude = model.Longitude!.Value,
                Comment = model.Comment.Trim(),
                Timestamp = timeProvider.GetUtcNow().UtcDateTime,
                Status = TransportStatus.New
            };

            appDbContext.Requests.Add(request);
            await appDbContext.SaveChangesAsync();
            await notifier.NotifyAsync(new TransportChange(request.Id, request.Status, TransportChangeKind.Created), [request.Email]);
            return new BaseResponse(true, localizer["Transport.Added"]);
        }

        // Gives a request to an executor, which also puts it in progress.
        public async Task<BaseResponse> AssignAsync(int id, string assigneeEmail, TransportActor actor)
        {
            var request = await appDbContext.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (!actor.IsAdmin)
            {
                return Forbidden();
            }

            if (!TransportWorkflow.CanAssign(request.Status, actor))
            {
                return WrongState();
            }

            var email = assigneeEmail.Trim().ToLowerInvariant();
            if (!await appDbContext.Users.AnyAsync(u => u.Email == email))
            {
                return new BaseResponse(false, localizer["Account.NotFound"], ResponseError.NotFound);
            }

            var previousAssignee = request.AssignedToEmail;
            request.AssignedToEmail = email;
            request.Status = TransportStatus.InProgress;
            return await SaveChangeAsync(request, "Transport.Assigned", previousAssignee);
        }

        public async Task<BaseResponse> CompleteAsync(int id, TransportActor actor)
        {
            var request = await appDbContext.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != TransportStatus.InProgress)
            {
                return WrongState();
            }

            if (!TransportWorkflow.CanComplete(request.Status, request.AssignedToEmail, actor))
            {
                return Forbidden();
            }

            request.Status = TransportStatus.Completed;
            return await SaveChangeAsync(request, "Transport.Completed");
        }

        public async Task<BaseResponse> CancelAsync(int id, TransportActor actor)
        {
            var request = await appDbContext.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (!TransportWorkflow.IsOpen(request.Status))
            {
                return WrongState();
            }

            if (!TransportWorkflow.CanCancel(request.Status, request.Email, actor))
            {
                return Forbidden();
            }

            request.Status = TransportStatus.Cancelled;
            return await SaveChangeAsync(request, "Transport.Cancelled");
        }

        private async Task<BaseResponse> SaveChangeAsync(TransportRequests request, string messageKey, string? previousAssignee = null)
        {
            request.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            try
            {
                await appDbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return new BaseResponse(false, localizer["Transport.Stale"], ResponseError.Conflict);
            }

            await notifier.NotifyAsync(
                new TransportChange(request.Id, request.Status, TransportChangeKind.Updated),
                [request.Email, request.AssignedToEmail, previousAssignee]);
            return new BaseResponse(true, localizer[messageKey]);
        }

        private BaseResponse NotFound() => new(false, localizer["Transport.NotFound"], ResponseError.NotFound);

        private BaseResponse Forbidden() => new(false, localizer["Transport.Forbidden"], ResponseError.Forbidden);

        private BaseResponse WrongState() => new(false, localizer["Transport.WrongState"], ResponseError.Conflict);
    }
}
