using Microsoft.AspNetCore.Mvc;
using Moq;
using TrackWeatherWeb.Controllers;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Repositories;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace UnitTests
{
	public class TestsForAccount
	{
		[Fact]
		public async Task GetUsersAsync_ShouldReturnOkObjectResult_WithListOfUsers()
		{
			// Arrange
			var mockRepo = new Mock<IAccount>();
			var mockUsers = new List<GetUsersDTO>
		{
			new GetUsersDTO { Name = "User1", Email = "user1@example.com", Role = "User" },
			new GetUsersDTO { Name = "User2", Email = "user2@example.com", Role = "User" }
		};

			mockRepo.Setup(repo => repo.GetUsersAsync()).ReturnsAsync(mockUsers);

			var controller = new ApplicationController(mockRepo.Object, null!);

			// Act
			var result = await controller.GetUsersAsync();

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var returnValue = Assert.IsType<List<GetUsersDTO>>(okResult.Value);
			Assert.Equal(2, returnValue.Count);
		}
		[Fact]
		public async Task GetUserAsync_ShouldReturnOkObjectResult_WithApplicationUser()
		{
			// Arrange
			var mockRepo = new Mock<IAccount>();
			var email = "user1@example.com";
			var user = new ApplicationUsers
			{
				Name = "User1",
				Email = email,
				Role = "User",
				Password = "hashed_password"
			};

			// Configure mockRepo to return the expected user when requested by email
			mockRepo.Setup(repo => repo.GetUserAsync(email)).ReturnsAsync(user);

			var controller = new ApplicationController(mockRepo.Object, null!);

			// Act
			var result = await controller.GetUserAsync(email);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var returnValue = Assert.IsType<ApplicationUsers>(okResult.Value);

			Assert.Equal(user.Name, returnValue.Name);
			Assert.Equal(user.Email, returnValue.Email);
			Assert.Equal(user.Role, returnValue.Role);
		}

		[Fact]
		public async Task RegisterAsync_ShouldReturnOkObjectResult_WithRegisterResponse()
		{
			// Arrange
			var mockRepo = new Mock<IAccount>();
			var registerDTO = new RegisterDTO
			{
				Name = "User1",
				Email = "user1@example.com",
				Password = "Pass123",
				Role = "User"
			};
			var registerResponse = new RegisterResponse(true, "User registered successfully");

			mockRepo.Setup(repo => repo.RegisterAsync(registerDTO)).ReturnsAsync(registerResponse);

			var controller = new ApplicationController(mockRepo.Object, null!);

			// Act
			var result = await controller.RegisterAsync(registerDTO);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var returnValue = Assert.IsType<RegisterResponse>(okResult.Value);

			Assert.True(returnValue.Flag);
			Assert.Equal("User registered successfully", returnValue.Message);
		}

		[Fact]
		public async Task LoginAsync_ShouldReturnOkObjectResult_WithLoginResponse()
		{
			// Arrange
			var mockRepo = new Mock<IAccount>();
			var loginDto = new LoginDTO
			{
				Email = "user1@example.com",
				Password = "Pass123"
			};
			var loginResponse = new LoginResponse(true, "Login successfully", "fake_jwt_token");

			mockRepo.Setup(repo => repo.LoginAsync(loginDto)).ReturnsAsync(loginResponse);

			var controller = new ApplicationController(mockRepo.Object, null!);

			// Act
			var result = await controller.LoginAsync(loginDto);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var returnValue = Assert.IsType<LoginResponse>(okResult.Value);

			Assert.True(returnValue.Flag);
			Assert.Equal("Login successfully", returnValue.Message);
			Assert.NotNull(returnValue.JWTToken);
			Assert.Equal("fake_jwt_token", returnValue.JWTToken);
		}

		[Fact]
		public async Task DeleteAsync_ShouldReturnOkObjectResult()
		{
			// Arrange
			var mockRepo = new Mock<IAccount>();

			var deleteUserDTO = new DeleteUserDTO
			{
				Email = "user1@example.com",
			};


			var deleteResponse = new BaseResponse(true, "User deleted successfully");

			mockRepo.Setup(repo => repo.DeleteUserAsync(deleteUserDTO)).ReturnsAsync(deleteResponse);

			var controller = new ApplicationController(mockRepo.Object, null!);

			// Act
			var result = await controller.DeleteUserAsync(deleteUserDTO);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var actualResponse = Assert.IsType<BaseResponse>(okResult.Value);
			Assert.True(actualResponse.Flag);
			Assert.Equal("User deleted successfully", actualResponse.Message);
		}
		[Fact]
		public async Task UpdateUser_ShouldReturnOkObjectResult_WithSuccessMessage()
		{
			// Arrange
			var mockRepo = new Mock<IAccount>();

			var email = "user1@example.com";
			var updateUserDTO = new RegisterDTO
			{
				Name = "Updated Name",
				Role = "Admin",
				Password = "NewPassword123",
				ConfirmPassword = "NewPassword123"
			};

			var updateResponse = new RegisterResponse(true, "User updated successfully.");

			mockRepo.Setup(repo => repo.UpdateUserAsync(email, updateUserDTO))
					.ReturnsAsync(updateResponse);

			var controller = new ApplicationController(mockRepo.Object, null!);

			// Act
			var result = await controller.UpdateUserAsync(email, updateUserDTO);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var actualResponse = Assert.IsType<RegisterResponse>(okResult.Value);

			Assert.True(actualResponse.Flag);
			Assert.Equal("User updated successfully.", actualResponse.Message);

			mockRepo.Verify(repo => repo.UpdateUserAsync(email, updateUserDTO), Times.Once);
		}
	}
}
