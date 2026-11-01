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
    [Route("api/users")]
    [Authorize(Roles = Roles.Admin)]
    public class AccountController(IAccountRepo _accountRepo) : ApiControllerBase
    {
        [HttpGet]
        [SwaggerOperation(Summary = "Get all users")]
        [ProducesResponseType<List<GetUsersDTO>>(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetUsersAsync()
        {
            var result = await _accountRepo.GetUsersAsync();
            return Ok(result);
        }

        [HttpGet("{email}")]
        [SwaggerOperation(Summary = "Get one user by email")]
        [ProducesResponseType<GetUsersDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetUserAsync(string email)
        {
            var result = await _accountRepo.GetUserAsync(email);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create a user with a chosen role")]
        [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> CreateUserAsync(CreateUserDTO model)
        {
            var result = await _accountRepo.CreateUserAsync(model);
            return FromResponse(result, StatusCodes.Status201Created);
        }

        [HttpPut("{email}")]
        [SwaggerOperation(
           Summary = "Update a user",
           Description = "Updates the name, role or password. Omitted fields stay unchanged."
        )]
        [ProducesResponseType<RegisterResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateUserAsync(string email, UpdateUserDTO model)
        {
            var result = await _accountRepo.UpdateUserAsync(email, model);
            return FromResponse(result);
        }

        [HttpDelete("{email}")]
        [SwaggerOperation(
          Summary = "Delete a user",
          Description = "Your own account and the last administrator cannot be deleted."
        )]
        [ProducesResponseType<BaseResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DeleteUserAsync(string email)
        {
            var result = await _accountRepo.DeleteUserAsync(email, User.FindFirstValue(ClaimTypes.Email));
            return FromResponse(result);
        }
    }
}
