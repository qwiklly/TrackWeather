using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Controllers
{
    [Route("api/transport-requests")]
    public class TransportController(ITransportRepo _transportrepo) : ApiControllerBase
    {
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [SwaggerOperation(Summary = "Get all transport requests", Description = "Newest first.")]
        [ProducesResponseType<List<RequestTransportDTO>>(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetTransportRequestsAsync()
        {
            var result = await _transportrepo.GetTransportRequestsAsync();
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
        [SwaggerOperation(
            Summary = "Request a transport",
            Description = "Requests a special transport to a point. The author and time are set by the server."
        )]
        [ProducesResponseType<BaseResponse>(StatusCodes.Status201Created)]
        public async Task<ActionResult> RequestTransportAsync(CreateTransportRequestDTO model)
        {
            var result = await _transportrepo.RequestTransportAsync(User.FindFirstValue(ClaimTypes.Email)!, model);
            return FromResponse(result, StatusCodes.Status201Created);
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
