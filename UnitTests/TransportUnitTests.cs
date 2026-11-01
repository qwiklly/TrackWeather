using Microsoft.AspNetCore.Mvc;
using Moq;
using TrackWeatherWeb.Controllers;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace UnitTests
{
	public class TestsForTransportRequests
	{
		[Fact]
		public async Task GetTransportRequestsAsync_ShouldReturnOkObjectResult_WithListOfRequests()
		{
			// Arrange
			var mockRepo = new Mock<ITransportRepo>();
			var mockTransport = new List<RequestTransportDTO>
			{
				new RequestTransportDTO { Id = 1, Email = "user1@example.com", Latitude = 48.909172m, Longitude = 40.467272m, Comment = "Raining, need transport" },
				new RequestTransportDTO { Id = 2, Email = "user2@example.com", Latitude = 49.909172m, Longitude = 41.467272m, Comment = "Raining, need transport" }
			};

			mockRepo.Setup(repo => repo.GetTransportRequestsAsync()).ReturnsAsync(mockTransport);

			var controller = new TransportController(mockRepo.Object);

			// Act
			var result = await controller.GetTransportRequestsAsync();

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result);
			var returnValue = Assert.IsType<List<RequestTransportDTO>>(okResult.Value);
			Assert.Equal(2, returnValue.Count);
		}

		[Fact]
		public async Task GetTransportRequestAsync_ShouldReturnNotFound_WhenMissing()
		{
			// Arrange
			var mockRepo = new Mock<ITransportRepo>();
			var controller = new TransportController(mockRepo.Object);

			// Act
			var result = await controller.GetTransportRequestAsync(404);

			// Assert
			Assert.IsType<NotFoundResult>(result);
		}

		[Fact]
		public async Task DeleteTransportRequestAsync_ShouldReturnNotFound_WhenRepositoryReportsIt()
		{
			// Arrange
			var mockRepo = new Mock<ITransportRepo>();
			mockRepo.Setup(repo => repo.DeleteTransportRequestAsync(1))
				.ReturnsAsync(new BaseResponse(false, "Missing", ResponseError.NotFound));

			var controller = new TransportController(mockRepo.Object);

			// Act
			var result = await controller.DeleteTransportRequestAsync(1);

			// Assert
			var objectResult = Assert.IsType<ObjectResult>(result);
			Assert.Equal(404, objectResult.StatusCode);
		}

		[Fact]
		public async Task UpdateTransportRequest_ShouldCallRepository_AndReturnOk()
		{
			// Arrange
			var mockRepo = new Mock<ITransportRepo>();
			var id = 2;
			var model = new CreateTransportRequestDTO { Latitude = 48.9m, Longitude = 40.4m, Comment = "Raining, need transport" };

			mockRepo.Setup(repo => repo.UpdateTransportRequestAsync(id, model))
				.ReturnsAsync(new BaseResponse(true, "Transport request updated successfully."));

			var controller = new TransportController(mockRepo.Object);

			// Act
			var result = await controller.UpdateTransportRequestAsync(id, model);

			// Assert
			Assert.IsType<OkObjectResult>(result);
			mockRepo.Verify(repo => repo.UpdateTransportRequestAsync(id, model), Times.Once);
		}
	}
}
