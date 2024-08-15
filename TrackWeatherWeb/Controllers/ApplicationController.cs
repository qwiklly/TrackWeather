using Microsoft.AspNetCore.Mvc;
using static TrackWeatherWeb.Responses.CustomResponses;
using Swashbuckle.AspNetCore.Annotations;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories;

namespace TrackWeatherWeb.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationController(IApplication _accountrepo) : ControllerBase
    {
        [HttpGet("GetUsers")]
        [SwaggerOperation(
            Summary = "Get all users",
            Description = "Retrieve a list of all users from the database."
        )]
        public async Task<ActionResult<BaseResponse>> GetUsersAsync()
        {
            var result = await _accountrepo.GetUsersAsync();
            return Ok(result);
        }
        [HttpGet("GetUser")]
        [SwaggerOperation(
            Summary = "Get one user by id",
            Description = "Retrieve a single user from the database using their email address."
        )]
        public async Task<ActionResult<BaseResponse>> GetUserAsync(string email)
        {
            var result = await _accountrepo.GetUserAsync(email);
            return Ok(result);
        }

        [HttpGet("GetCoordinates")]
        [SwaggerOperation(
            Summary = "Get all transport requests",
            Description = "Retrieve a list of all transport requests (coordinates) from the database."
        )]
        public async Task<ActionResult<BaseResponse>> GetCoordinatesAsync()
        {
            var result = await _accountrepo.GetCoordinatesAsync();
            return Ok(result);
        }

        [HttpGet("GetCoordinate")]
        [SwaggerOperation(
            Summary = "Get a transport request by ID",
            Description = "Retrieve a specific transport request (coordinates) from the database using its ID."
        )]
        public async Task<ActionResult<BaseResponse>> GetCoordinatesAsync(int id)
        {
            var result = await _accountrepo.GetCoordinateAsync(id);
            return Ok(result);
        }

        [HttpPost("Register")]
        [SwaggerOperation(
            Summary = "Register a new user",
            Description = "Registers a new user."
        )]
        public async Task<ActionResult<RegisterResponse>> RegisterAsync(RegisterDTO model)
        {
            var result = await _accountrepo.RegisterAsync(model);
            return Ok(result);
        }

        [HttpPost("Login")]
        [SwaggerOperation(
            Summary = "Login a user",
            Description = "Authenticate a user with email and password."
        )]
        public async Task<ActionResult<LoginResponse>> LoginAsync(LoginDTO model)
        {
            var result = await _accountrepo.LoginAsync(model);
            return Ok(result);
        }

        [HttpPost("RequestTransport")]
        [SwaggerOperation(
            Summary = "Request a transport",
            Description = "Request a special transport to x and y coordinates. Current data is created automatically."
        )]
        public async Task<ActionResult<BaseResponse>> RequestTransportAsync(RequestTransportDTO model)
        {
            var result = await _accountrepo.RequestTransportAsync(model);
            return Ok(result);
        }

        [HttpDelete("DeleteUser")]
        [SwaggerOperation(
            Summary = "Delete a user",
            Description = "Delete a user."
        )]
        public async Task<ActionResult<BaseResponse>> DeleteUser(DeleteUserDTO model)
        {
            var result = await _accountrepo.DeleteUserAsync(model);
            return Ok(result);
        }

        [HttpDelete("DeleteCoordinates")]
        [SwaggerOperation(
            Summary = "Delete transport coordinates.",
            Description = "Delete transport coordinates."
        )]
        public async Task<ActionResult<BaseResponse>> DeleteCoordinates(DeleteTransportRequestDTO model)
        {
            var result = await _accountrepo.DeleteCoordinatesAsync(model);
            return Ok(result);
        }

        [HttpPut("UpdateUser/{email}")]
        [SwaggerOperation(
            Summary = "Update a user",
            Description = "Update a user's information, such as name, role, or password."
        )]
        public async Task<ActionResult<BaseResponse>> UpdateUser(string email, RegisterDTO model)
        {
            var result = await _accountrepo.UpdateUserAsync(email, model);
            return Ok(result);
        }

        [HttpPut("UpdateRequest/{id}")]
        [SwaggerOperation(
            Summary = "Update transport request.",
            Description = "Update the details of an existing transport request (coordinates) by its ID."
        )]
        public async Task<ActionResult<BaseResponse>> DeleteCoordinates(int id, RequestTransportDTO model)
        {
            var result = await _accountrepo.UpdateCoordinatesAsync(id, model);
            return Ok(result);
        }
    }
}


