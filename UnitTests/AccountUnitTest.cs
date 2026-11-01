using Microsoft.AspNetCore.Mvc;
using Moq;
using TrackWeatherWeb.Controllers;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace UnitTests
{
	public class TestsForAccount
	{
		[Fact]
		public async Task GetUsersAsync_ShouldReturnOkObjectResult_WithListOfUsers()
		{
			// Arrange
			var mockRepo = new Mock<IAccountRepo>();
			var mockUsers = new List<GetUsersDTO>
			{
				new GetUsersDTO { Name = "User1", Email = "user1@example.com", Role = "User" },
				new GetUsersDTO { Name = "User2", Email = "user2@example.com", Role = "User" }
			};

			mockRepo.Setup(repo => repo.GetUsersAsync()).ReturnsAsync(mockUsers);

			var controller = new AccountController(mockRepo.Object);

			// Act
			var result = await controller.GetUsersAsync();

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result);
			var returnValue = Assert.IsType<List<GetUsersDTO>>(okResult.Value);
			Assert.Equal(2, returnValue.Count);
		}

		[Fact]
		public async Task GetUserAsync_ShouldReturnUser_WhenFound()
		{
			// Arrange
			var mockRepo = new Mock<IAccountRepo>();
			var email = "user1@example.com";
			var user = new GetUsersDTO { Name = "User1", Email = email, Role = "User" };

			mockRepo.Setup(repo => repo.GetUserAsync(email)).ReturnsAsync(user);

			var controller = new AccountController(mockRepo.Object);

			// Act
			var result = await controller.GetUserAsync(email);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result);
			var returnValue = Assert.IsType<GetUsersDTO>(okResult.Value);
			Assert.Equal(user.Email, returnValue.Email);
		}

		[Fact]
		public async Task GetUserAsync_ShouldReturnNotFound_WhenMissing()
		{
			// Arrange
			var mockRepo = new Mock<IAccountRepo>();
			var controller = new AccountController(mockRepo.Object);

			// Act
			var result = await controller.GetUserAsync("missing@example.com");

			// Assert
			Assert.IsType<NotFoundResult>(result);
		}

		[Fact]
		public async Task RegisterAsync_ShouldReturnCreated_WhenRegistered()
		{
			// Arrange
			var mockRepo = new Mock<IAccountRepo>();
			var registerDTO = new RegisterDTO { Name = "User1", Email = "user1@example.com", Password = "Passw0rd" };
			var registerResponse = new RegisterResponse(true, "User registered successfully");

			mockRepo.Setup(repo => repo.RegisterAsync(registerDTO)).ReturnsAsync(registerResponse);

			var controller = new AuthController(mockRepo.Object);

			// Act
			var result = await controller.RegisterAsync(registerDTO);

			// Assert
			var objectResult = Assert.IsType<ObjectResult>(result);
			Assert.Equal(201, objectResult.StatusCode);
			Assert.Equal(registerResponse, objectResult.Value);
		}

		[Fact]
		public async Task RegisterAsync_ShouldReturnConflict_WhenEmailIsTaken()
		{
			// Arrange
			var mockRepo = new Mock<IAccountRepo>();
			var registerDTO = new RegisterDTO { Email = "user1@example.com" };

			mockRepo.Setup(repo => repo.RegisterAsync(registerDTO))
				.ReturnsAsync(new RegisterResponse(false, "Taken", ResponseError.Conflict));

			var controller = new AuthController(mockRepo.Object);

			// Act
			var result = await controller.RegisterAsync(registerDTO);

			// Assert
			var objectResult = Assert.IsType<ObjectResult>(result);
			Assert.Equal(409, objectResult.StatusCode);
			Assert.Equal("Taken", Assert.IsType<ProblemDetails>(objectResult.Value).Detail);
		}

		[Fact]
		public async Task LoginAsync_ShouldReturnOkObjectResult_WithLoginResponse()
		{
			// Arrange
			var mockRepo = new Mock<IAccountRepo>();
			var loginDto = new LoginDTO { Email = "user1@example.com", Password = "Passw0rd" };
			var loginResponse = new LoginResponse(true, "Login successfully", "fake_jwt_token");

			mockRepo.Setup(repo => repo.LoginAsync(loginDto)).ReturnsAsync(loginResponse);

			var controller = new AuthController(mockRepo.Object);

			// Act
			var result = await controller.LoginAsync(loginDto);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result);
			var returnValue = Assert.IsType<LoginResponse>(okResult.Value);
			Assert.True(returnValue.Flag);
			Assert.Equal("fake_jwt_token", returnValue.JWTToken);
		}

		[Fact]
		public async Task UpdateUser_ShouldCallRepository_AndReturnOk()
		{
			// Arrange
			var mockRepo = new Mock<IAccountRepo>();
			var email = "user1@example.com";
			var updateUserDTO = new UpdateUserDTO { Name = "Updated Name", Role = "Admin" };

			mockRepo.Setup(repo => repo.UpdateUserAsync(email, updateUserDTO))
				.ReturnsAsync(new RegisterResponse(true, "User updated successfully."));

			var controller = new AccountController(mockRepo.Object);

			// Act
			var result = await controller.UpdateUserAsync(email, updateUserDTO);

			// Assert
			Assert.IsType<OkObjectResult>(result);
			mockRepo.Verify(repo => repo.UpdateUserAsync(email, updateUserDTO), Times.Once);
		}
	}
}
