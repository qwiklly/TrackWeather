using Microsoft.AspNetCore.Mvc;
using Moq;
using TrackWeatherWeb.Controllers;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Repositories;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace UnitTests
{
	public class TestsForTransportRequests
	{
		[Fact]
		public async Task GetAllTransportRequestsAsync_ShouldReturnOkObjectResult_WithListOfRequests()
		{
			// Arrange
			var mockRepo = new Mock<ITransport>();
			var mockTransport = new List<RequestTransportDTO>
		{
			new RequestTransportDTO { Id = 1, Email = "user1@example.com", Coordinate_x = 48.909172455411614m, Coordinate_y =  40.46727250624546m, Comment = "Raining, need transport", Timestamp = DateTime.Parse("2024-08-27 10:30:00")},
			new RequestTransportDTO  { Id = 2, Email = "user2@example.com", Coordinate_x = 49.909172455411614m, Coordinate_y =  41.46727250624546m, Comment = "Raining, need transport", Timestamp = DateTime.Parse("2024-08-27 10:35:00")}
		};

			mockRepo.Setup(repo => repo.GetTransportRequestsAsync()).ReturnsAsync(mockTransport);

			var controller = new ApplicationController(null!, mockRepo.Object);

			// Act
			var result = await controller.GetTransportRequestsAsync();

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var returnValue = Assert.IsType<List<RequestTransportDTO>>(okResult.Value);
			Assert.Equal(2, returnValue.Count);
		}
		[Fact]
		public async Task GetTransportAsync_ShouldReturnOkObjectResult_WithTransportRequests()
		{
			// Arrange
			var mockRepo = new Mock<ITransport>();
			int id = 1;
			var transport = new TransportRequests
			{
				Email = "user1@example.com",
				Coordinate_x = 48.909172455411614m,
				Coordinate_y = 40.46727250624546m,
				Comment = "Raining, need transport",
				Timestamp = DateTime.Parse("2024-08-27 10:30:00")
			};

			mockRepo.Setup(repo => repo.GetTransportRequestAsync(id)).ReturnsAsync(transport);

			var controller = new ApplicationController(null!, mockRepo.Object);

			// Act
			var result = await controller.GetTransportRequestAsync(id);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var returnValue = Assert.IsType<TransportRequests>(okResult.Value);

			Assert.Equal(transport.Id, returnValue.Id);
			Assert.Equal(transport.Email, returnValue.Email);
			Assert.Equal(transport.Coordinate_x, returnValue.Coordinate_x);
			Assert.Equal(transport.Coordinate_y, returnValue.Coordinate_y);
			Assert.Equal(transport.Comment, returnValue.Comment);
			Assert.Equal(transport.Timestamp, returnValue.Timestamp);
		}

		[Fact]
		public async Task AddTransportRequestAsync_ShouldReturnOkObjectResult_WithRegisterResponse()
		{
			// Arrange
			var mockRepo = new Mock<ITransport>();
			var requestTransportDTO = new RequestTransportDTO
			{
				Id = 1,
				Email = "user1@example.com",
				Coordinate_x = 48.909172455411614m,
				Coordinate_y = 40.46727250624546m,
				Comment = "Raining, need transport",
				Timestamp = DateTime.Parse("2024-08-27 10:30:00")
			};
			var requestTransportResponse = new BaseResponse(true, "Transport request added successfully");

			mockRepo.Setup(repo => repo.RequestTransportAsync(requestTransportDTO)).ReturnsAsync(requestTransportResponse);

			var controller = new ApplicationController(null!, mockRepo.Object);

			// Act
			var result = await controller.RequestTransportAsync(requestTransportDTO);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var returnValue = Assert.IsType<BaseResponse>(okResult.Value);

			Assert.True(returnValue.Flag);
			Assert.Equal("Transport request added successfully", returnValue.Message);
		}

		[Fact]
		public async Task DeleteTransportRequestAsync_ShouldReturnOkObjectResult()
		{
			// Arrange
			var mockRepo = new Mock<ITransport>();

			var deleteTransportRequestDTO = new DeleteTransportRequestDTO
			{
				Id = 1
			};

			var expectedResponse = new BaseResponse(true, "Transport request deleted successfully");

			mockRepo.Setup(repo => repo.DeleteTransportRequestAsync(deleteTransportRequestDTO)).ReturnsAsync(expectedResponse);

			var controller = new ApplicationController(null!, mockRepo.Object);

			// Act
			var result = await controller.DeleteTransportRequestAsync(deleteTransportRequestDTO);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var actualResponse = Assert.IsType<BaseResponse>(okResult.Value);
			Assert.True(actualResponse.Flag);
			Assert.Equal("Transport request deleted successfully", actualResponse.Message);
		}

		[Fact]
		public async Task UpdateTransportRequest_ShouldReturnOkObjectResult_WithSuccessMessage()
		{
			// Arrange
			var mockRepo = new Mock<ITransport>();

			var id = 2;
			var updateTransportDTO = new RequestTransportDTO
			{
				Id = 1,
				Email = "user1@example.com",
				Coordinate_x = 48.909172455411614m,
				Coordinate_y = 40.46727250624546m,
				Comment = "Raining, need transport",
				Timestamp = DateTime.Parse("2024-08-27 10:30:00")
			};

			var updateTransportResponse = new BaseResponse(true, "Transport request updated successfully.");

			mockRepo.Setup(repo => repo.UpdateTransportRequestAsync(id, updateTransportDTO))
			.ReturnsAsync(updateTransportResponse);

			var controller = new ApplicationController(null!, mockRepo.Object);

			// Act
			var result = await controller.UpdateTransportRequestAsync(id, updateTransportDTO);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var actualResponse = Assert.IsType<BaseResponse>(okResult.Value);

			Assert.True(actualResponse.Flag);
			Assert.Equal("Transport request updated successfully.", actualResponse.Message);

			mockRepo.Verify(repo => repo.UpdateTransportRequestAsync(id, updateTransportDTO), Times.Once);
		}
	}
}
