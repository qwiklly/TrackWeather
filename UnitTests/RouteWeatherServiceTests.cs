using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using TrackWeatherWeb;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories;
using TrackWeatherWeb.Repositories.Interfaces;
using TrackWeatherWeb.Services;
using TrackWeatherWeb.WeatherModels;

namespace UnitTests
{
	public class RouteWeatherServiceTests
	{
		private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

		private sealed class FixedTime : TimeProvider
		{
			public override DateTimeOffset GetUtcNow() => new(Now);
		}

		private sealed class KeyLocalizer : IStringLocalizer<SharedResource>
		{
			public LocalizedString this[string name] => new(name, "text:" + name);
			public LocalizedString this[string name, params object[] arguments] => this[name];
			public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
		}

		private sealed class FakeRepo(Func<double, double, IReadOnlyList<ForecastItem>> forecast) : IWeatherRepo
		{
			public int Calls;

			public Task<WeatherDTO> GetCurrentWeatherAsync(double lat, double lon, string language) => throw new NotSupportedException();

			public Task<IReadOnlyList<ForecastItem>> GetForecastAsync(double lat, double lon, string language)
			{
				Interlocked.Increment(ref Calls);
				return Task.FromResult(forecast(lat, lon));
			}
		}

		private static ForecastItem Calm(DateTime time) => new()
		{
			Dt = new DateTimeOffset(time).ToUnixTimeSeconds(),
			Main = new ForecastMain { Temp = 15, Humidity = 40 },
			Weather = [new WeatherDescription { Id = 800, Description = "clear" }]
		};

		private static ForecastItem Icy(DateTime time) => new()
		{
			Dt = new DateTimeOffset(time).ToUnixTimeSeconds(),
			Main = new ForecastMain { Temp = -1, Humidity = 90 },
			Weather = [new WeatherDescription { Id = 500, Description = "rain" }],
			Rain = new ForecastPrecipitation { ThreeHours = 1 },
			Pop = 0.9
		};

		private static RouteWeatherService Service(IWeatherRepo repo) =>
			new(repo, new KeyLocalizer(), new FixedTime(), NullLogger<RouteWeatherService>.Instance);

		// About 111 km north in two hours
		private static RouteWeatherRequestDTO Request(DateTime? departure = null) => new()
		{
			Departure = departure,
			Points =
			[
				new RoutePointDTO { Lat = 50, Lon = 39, Seconds = 0 },
				new RoutePointDTO { Lat = 51, Lon = 39, Seconds = 7200 }
			]
		};

		[Fact]
		public async Task EachPoint_UsesTheForecastForItsArrivalTime()
		{
			// Ice only after 13:30, so stretches reached later are the dangerous ones
			var service = Service(new FakeRepo((_, _) =>
			[
				Calm(Now),
				Icy(Now.AddHours(3)),
			]));

			var result = await service.AnalyzeAsync(Request(), "en", CancellationToken.None);

			Assert.True(result.Points.Count >= 2);
			Assert.Equal(RiskLevel.Low, result.Points[0].Risk);
			Assert.Equal(RiskLevel.High, result.Points[^1].Risk);
			Assert.Equal(RiskLevel.High, result.OverallRisk);
			Assert.Equal("text:Hazard.Ice", result.Points[^1].Hazards.Single().Title);
		}

		[Fact]
		public async Task LaterDeparture_ChangesTheAssessment()
		{
			var service = Service(new FakeRepo((_, _) => [Calm(Now), Icy(Now.AddHours(25))]));

			var now = await service.AnalyzeAsync(Request(), "en", CancellationToken.None);
			var tomorrow = await service.AnalyzeAsync(Request(Now.AddHours(24)), "en", CancellationToken.None);

			Assert.Equal(RiskLevel.Low, now.OverallRisk);
			Assert.Equal(RiskLevel.High, tomorrow.OverallRisk);
			Assert.Equal(Now.AddHours(24), tomorrow.DepartureUtc);
		}

		[Fact]
		public async Task DepartureInThePast_MeansNow()
		{
			var service = Service(new FakeRepo((_, _) => [Calm(Now)]));

			var result = await service.AnalyzeAsync(Request(Now.AddDays(-3)), "en", CancellationToken.None);

			Assert.Equal(Now, result.DepartureUtc);
		}

		[Fact]
		public async Task TimeBeyondTheForecast_IsUnknown_NotSafe()
		{
			var service = Service(new FakeRepo((_, _) => [Calm(Now)]));

			var result = await service.AnalyzeAsync(Request(Now.AddDays(10)), "en", CancellationToken.None);

			Assert.All(result.Points, p => Assert.Equal(RiskLevel.Unknown, p.Risk));
			Assert.Equal(RiskLevel.Unknown, result.OverallRisk);
		}

		[Fact]
		public async Task OneFailedPoint_DoesNotHideTheRest()
		{
			var repo = new FakeRepo((lat, _) => lat > 50.5
				? throw new WeatherUnavailableException("down")
				: [Calm(Now)]);

			var result = await Service(repo).AnalyzeAsync(Request(), "en", CancellationToken.None);

			Assert.Contains(result.Points, p => p.Risk == RiskLevel.Low);
			Assert.Contains(result.Points, p => p.Risk == RiskLevel.Unknown);
		}

		[Fact]
		public async Task TotalOutage_IsAnError()
		{
			var repo = new FakeRepo((_, _) => throw new WeatherUnavailableException("down"));

			await Assert.ThrowsAsync<WeatherUnavailableException>(
				() => Service(repo).AnalyzeAsync(Request(), "en", CancellationToken.None));
		}

		[Fact]
		public async Task ApiCalls_AreBoundedByTheSampleLimit()
		{
			var repo = new FakeRepo((_, _) => [Calm(Now)]);
			var request = new RouteWeatherRequestDTO
			{
				Points =
				[
					new RoutePointDTO { Lat = 40, Lon = 39, Seconds = 0 },
					new RoutePointDTO { Lat = 70, Lon = 39, Seconds = 100000 }
				]
			};

			await Service(repo).AnalyzeAsync(request, "en", CancellationToken.None);

			Assert.Equal(RouteSampler.MaxSamples, repo.Calls);
		}
	}
}
