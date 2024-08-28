using Microsoft.AspNetCore.Mvc;
using static TrackWeatherWeb.Responses.CustomResponses;
using Swashbuckle.AspNetCore.Annotations;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories;

namespace TrackWeatherWeb.Controllers
{

    [Route("api/application")]
    [ApiController]
    public class ApplicationController(IAccount _accountrepo, ITransport _transportrepo) : ControllerBase
    {
        [HttpGet("getWeather/{lat},{lon}")]
		[SwaggerOperation(
			Summary = "Get current weather",
			Description = "Retrieve info about current weather."
		)]
		public async Task<ActionResult<BaseResponse>> GetWeather1(double lat, double lon)
        {
            try
            {
                var result = await _transportrepo.GetCurrentWeatherAsync(lat, lon);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting weather: {ex.Message}");
            }
        }
        [HttpGet("getUsers")]
        [SwaggerOperation(
            Summary = "Get all users",
            Description = "Retrieve a list of all users from the database."
        )]
        public async Task<ActionResult<BaseResponse>> GetUsersAsync()
        {
            var result = await _accountrepo.GetUsersAsync();
            return Ok(result);
        }
        [HttpGet("getUser")]
        [SwaggerOperation(
            Summary = "Get one user by id",
            Description = "Retrieve a single user from the database using their email address."
        )]
        public async Task<ActionResult<BaseResponse>> GetUserAsync(string email)
        {
            var result = await _accountrepo.GetUserAsync(email);
            return Ok(result);
        }

        [HttpGet("getAllTransportRequests")]
        [SwaggerOperation(
            Summary = "Get all transport requests",
            Description = "Retrieve a list of all transport requests (coordinates) from the database."
        )]
        public async Task<ActionResult<BaseResponse>> GetTransportRequestsAsync()
        {
            var result = await _transportrepo.GetTransportRequestsAsync();
            return Ok(result);
        }

        [HttpGet("getTransportRequest")]
        [SwaggerOperation(
            Summary = "Get a transport request by ID",
            Description = "Retrieve a specific transport request (coordinates) from the database using its ID."
        )]
        public async Task<ActionResult<BaseResponse>> GetTransportRequestAsync(int id)
        {
            var result = await _transportrepo.GetTransportRequestAsync(id);
            return Ok(result);
        }

        [HttpPost("register")]
        [SwaggerOperation(
            Summary = "Register a new user",
            Description = "Registers a new user."
        )]
        public async Task<ActionResult<RegisterResponse>> RegisterAsync(RegisterDTO model)
        {
            var result = await _accountrepo.RegisterAsync(model);
            return Ok(result);
        }

        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Login a user",
            Description = "Authenticate a user with email and password."
        )]
        public async Task<ActionResult<LoginResponse>> LoginAsync(LoginDTO model)
        {
            var result = await _accountrepo.LoginAsync(model);
            return Ok(result);
        }

        [HttpPost("requestTransport")]
        [SwaggerOperation(
            Summary = "Request a transport",
            Description = "Request a special transport to x and y coordinates. Current data is created automatically."
        )]
        public async Task<ActionResult<BaseResponse>> RequestTransportAsync(RequestTransportDTO model)
        {
            var result = await _transportrepo.RequestTransportAsync(model);
            return Ok(result);
        }

        [HttpDelete("deleteUser")]
        [SwaggerOperation(
            Summary = "Delete a user",
            Description = "Delete a user."
        )]
        public async Task<ActionResult<BaseResponse>> DeleteUserAsync(DeleteUserDTO model)
        {
            var result = await _accountrepo.DeleteUserAsync(model);
            return Ok(result);
        }

        [HttpDelete("deleteTransportRequest")]
        [SwaggerOperation(
            Summary = "Delete transport coordinates.",
            Description = "Delete transport coordinates."
        )]
        public async Task<ActionResult<BaseResponse>> DeleteTransportRequestAsync(DeleteTransportRequestDTO model)
        {
            var result = await _transportrepo.DeleteTransportRequestAsync(model);
            return Ok(result);
        }

        [HttpPut("updateUser/{email}")]
        [SwaggerOperation(
            Summary = "Update a user",
            Description = "Update a user's information, such as name, role, or password."
        )]
        public async Task<ActionResult<BaseResponse>> UpdateUserAsync(string email, RegisterDTO model)
        {
            var result = await _accountrepo.UpdateUserAsync(email, model);
            return Ok(result);
        }

        [HttpPut("updateTransportRequest/{id}")]
        [SwaggerOperation(
            Summary = "Update transport request.",
            Description = "Update the details of an existing transport request (coordinates) by its ID."
        )]
        public async Task<ActionResult<BaseResponse>> UpdateTransportRequestAsync(int id, RequestTransportDTO model)
        {
            var result = await _transportrepo.UpdateTransportRequestAsync(id, model);
            return Ok(result);
        }
    }
}


