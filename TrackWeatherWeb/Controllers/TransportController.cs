using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Repositories.Interfaces;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Controllers
{
    [Route("api/transport-requests")]
    public class TransportController(ITransportRepo _transportrepo) : ApiControllerBase
    {
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [SwaggerOperation(Summary = "Get transport requests", Description = "Newest first, optionally filtered by status.")]
        [ProducesResponseType<List<RequestTransportDTO>>(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetTransportRequestsAsync([FromQuery] TransportStatus? status = null)
        {
            var result = await _transportrepo.GetTransportRequestsAsync(status);
            return Ok(result);
        }

        // Any signed-in user may see their own requests; the fallback policy covers this action
        [HttpGet("mine")]
        [SwaggerOperation(Summary = "Get your transport requests", Description = "Requests you created or were assigned to.")]
        [ProducesResponseType<List<RequestTransportDTO>>(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetMyRequestsAsync()
        {
            var result = await _transportrepo.GetRequestsForUserAsync(User.FindFirstValue(ClaimTypes.Email)!);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [SwaggerOperation(Summary = "Get a transport request by ID")]
        [ProducesResponseType<RequestTransportDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTransportRequestAsync(int id)
        {
            var result = await _transportrepo.GetTransportRequestAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // Any signed-in user may ask for transport; the fallback policy covers this action
        [HttpPost]
        [EnableRateLimiting(AuthDefaults.UserActionsPolicy)]
        [SwaggerOperation(
            Summary = "Request a transport",
            Description = "Requests a special transport to a point. The author, time and status are set by the server."
        )]
        [ProducesResponseType<BaseResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult> RequestTransportAsync(CreateTransportRequestDTO model)
        {
            var result = await _transportrepo.RequestTransportAsync(User.FindFirstValue(ClaimTypes.Email)!, model);
            return FromResponse(result, StatusCodes.Status201Created);
        }

        [HttpPost("{id:int}/assign")]
        [Authorize(Roles = Roles.Admin)]
        [SwaggerOperation(Summary = "Assign an executor", Description = "Gives an open request to a user and puts it in progress.")]
        [ProducesResponseType<BaseResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AssignAsync(int id, AssignTransportRequestDTO model)
        {
            var result = await _transportrepo.AssignAsync(id, model.Email, User.ToTransportActor());
            return FromResponse(result);
        }

        [HttpPost("{id:int}/complete")]
        [SwaggerOperation(Summary = "Complete a request", Description = "Available to an administrator and to the assigned executor.")]
        [ProducesResponseType<BaseResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> CompleteAsync(int id)
        {
            var result = await _transportrepo.CompleteAsync(id, User.ToTransportActor());
            return FromResponse(result);
        }

        [HttpPost("{id:int}/cancel")]
        [SwaggerOperation(Summary = "Cancel a request", Description = "Available to an administrator; the author can cancel only until the request is taken.")]
        [ProducesResponseType<BaseResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> CancelAsync(int id)
        {
            var result = await _transportrepo.CancelAsync(id, User.ToTransportActor());
            return FromResponse(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [SwaggerOperation(Summary = "Update a transport request")]
        [ProducesResponseType<BaseResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateTransportRequestAsync(int id, CreateTransportRequestDTO model)
        {
            var result = await _transportrepo.UpdateTransportRequestAsync(id, model);
            return FromResponse(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [SwaggerOperation(Summary = "Delete a transport request")]
        [ProducesResponseType<BaseResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteTransportRequestAsync(int id)
        {
            var result = await _transportrepo.DeleteTransportRequestAsync(id);
            return FromResponse(result);
        }
    }
}
