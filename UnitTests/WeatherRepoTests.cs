using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using TrackWeatherWeb.Options;
using TrackWeatherWeb.Repositories;

namespace UnitTests
{
	public class WeatherRepoTests
	{
		private const string SampleResponse = """
			{ "name": "Воронеж", "main": { "temp": 7.4 }, "weather": [ { "main": "Clouds", "description": "пасмурно" } ] }
			""";

		private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
		{
			public List<Uri> Requests { get; } = [];

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				Requests.Add(request.RequestUri!);
				return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
			}
		}

		private static WeatherRepo CreateRepo(FakeHandler handler) => new(
			new HttpClient(handler) { BaseAddress = new Uri("https://weather.test") },
			Options.Create(new OpenWeatherOptions { ApiKey = "secret-key" }),
			new MemoryCache(new MemoryCacheOptions()));

		[Fact]
		public async Task Coordinates_AreSentWithDots_UnderRussianCulture()
		{
			var previous = CultureInfo.CurrentCulture;
			CultureInfo.CurrentCulture = new CultureInfo("ru");
			try
			{
				var handler = new FakeHandler(HttpStatusCode.OK, SampleResponse);

				await CreateRepo(handler).GetCurrentWeatherAsync(51.672, 39.184, "ru");

				var query = Assert.Single(handler.Requests).Query;
				Assert.Contains("lat=51.672", query);
				Assert.Contains("lon=39.184", query);
				Assert.Contains("lang=ru", query);
			}
			finally
			{
				CultureInfo.CurrentCulture = previous;
			}
		}

		[Fact]
		public async Task Response_IsMappedToDto()
		{
			var weather = await CreateRepo(new FakeHandler(HttpStatusCode.OK, SampleResponse))
				.GetCurrentWeatherAsync(51.672, 39.184, "ru");

			Assert.Equal(7.4, weather.Temp);
			Assert.Equal("пасмурно", weather.Summary);
			Assert.Equal("Воронеж", weather.City);
		}

		[Fact]
		public async Task SecondCall_ForTheSameCell_IsServedFromCache()
		{
			var handler = new FakeHandler(HttpStatusCode.OK, SampleResponse);
			var repo = CreateRepo(handler);

			await repo.GetCurrentWeatherAsync(51.6721, 39.1841, "ru");
			await repo.GetCurrentWeatherAsync(51.6722, 39.1842, "ru");

			Assert.Single(handler.Requests);
		}

		[Fact]
		public async Task Cache_IsSeparatePerLanguage()
		{
			var handler = new FakeHandler(HttpStatusCode.OK, SampleResponse);
			var repo = CreateRepo(handler);

			await repo.GetCurrentWeatherAsync(51.67, 39.18, "ru");
			await repo.GetCurrentWeatherAsync(51.67, 39.18, "en");

			Assert.Equal(2, handler.Requests.Count);
		}

		[Fact]
		public async Task Failure_DoesNotExposeTheApiKey()
		{
			var repo = CreateRepo(new FakeHandler(HttpStatusCode.Unauthorized, "{}"));

			var exception = await Assert.ThrowsAsync<WeatherUnavailableException>(
				() => repo.GetCurrentWeatherAsync(1, 2, "en"));

			Assert.DoesNotContain("secret-key", exception.Message);
		}
	}
}
