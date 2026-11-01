using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Controllers
{
    [Route("api/auth")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthDefaults.RateLimitPolicy)]
    public class AuthController(IAccountRepo _accountRepo) : ApiControllerBase
    {
        [HttpPost("register")]
        [SwaggerOperation(
           Summary = "Register a new user",
           Description = "Creates an account with the User role."
        )]
        [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RegisterAsync(RegisterDTO model)
        {
            var result = await _accountRepo.RegisterAsync(model);
            return FromResponse(result, StatusCodes.Status201Created);
        }

        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Log in",
            Description = "Exchanges an email and password for a JWT to send as `Authorization: Bearer <token>`."
        )]
        [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> LoginAsync(LoginDTO model)
        {
            var result = await _accountRepo.LoginAsync(model);
            return FromResponse(result);
        }
    }
}
