using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories;
using TrackWeatherWeb.Services;
using TrackWeatherWeb.WeatherModels;

namespace IntegrationTests
{
    public class RouteWeatherApiTests
    {
        private static object Route(int pointCount = 2) => new
        {
            points = Enumerable.Range(0, pointCount).Select(i => new
            {
                lat = 50 + 5.0 * i / Math.Max(pointCount - 1, 1),
                lon = 39.0,
                seconds = 36000.0 * i / Math.Max(pointCount - 1, 1)
            })
        };

        private static async Task<HttpClient> SignedInClientAsync(TestApp app, string? language = null)
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            await app.RegisterApiAsync(email);
            var client = app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await app.GetJwtAsync(email, TestApp.UserPassword));
            if (language != null)
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
            }
            return client;
        }

        private static IReadOnlyList<ForecastItem> IcyForecast() =>
            Enumerable.Range(0, 40).Select(i => new ForecastItem
            {
                Dt = DateTimeOffset.UtcNow.AddHours(3 * i).ToUnixTimeSeconds(),
                Main = new ForecastMain { Temp = -1, Humidity = 90 },
                Weather = [new WeatherDescription { Id = 500, Description = "rain" }],
                Rain = new ForecastPrecipitation { ThreeHours = 1 },
                Pop = 0.9
            }).ToList();

        [Fact]
        public async Task Anonymous_Gets401()
        {
            using var app = new TestApp();

            var response = await app.CreateClient().PostAsJsonAsync("/api/weather/route", Route());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("ru", "Гололёд")]
        [InlineData("en", "Ice")]
        public async Task Hazards_AreDescribedInTheRequestedLanguage(string language, string title)
        {
            using var app = new TestApp();
            app.Weather.Forecast = (_, _) => IcyForecast();
            var client = await SignedInClientAsync(app, language);

            var response = await client.PostAsJsonAsync("/api/weather/route", Route());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains(title, body);
            Assert.Contains("\"overallRisk\":\"High\"", body);
            Assert.Equal(language, app.Weather.LastLanguage);
        }

        [Fact]
        public async Task LongRoute_ReturnsStretchesThatMapBackToTheSubmittedLine()
        {
            using var app = new TestApp();
            app.Weather.Forecast = (_, _) => IcyForecast();
            var client = await SignedInClientAsync(app);

            var response = await client.PostAsJsonAsync("/api/weather/route", Route(500));
            var result = await response.Content.ReadFromJsonAsync<RouteWeatherDTO>();

            Assert.InRange(result!.Points.Count, 2, RouteSampler.MaxSamples);
            Assert.InRange(result.TotalKm, 500, 600);
            Assert.Equal(0, result.Points[0].StartIndex);
            Assert.Equal(499, result.Points[^1].EndIndex);
            Assert.All(result.Points, p => Assert.Equal(RiskLevel.High, p.Risk));
            Assert.All(result.Points, p => Assert.True(p.EtaUtc > result.DepartureUtc));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5001)]
        public async Task WrongNumberOfPoints_Returns400(int pointCount)
        {
            using var app = new TestApp();
            var client = await SignedInClientAsync(app);

            var response = await client.PostAsJsonAsync("/api/weather/route", Route(pointCount));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImpossibleCoordinates_Return400()
        {
            using var app = new TestApp();
            var client = await SignedInClientAsync(app);

            var response = await client.PostAsJsonAsync("/api/weather/route", new
            {
                points = new[] { new { lat = 120.0, lon = 39.0, seconds = 0.0 }, new { lat = 50.0, lon = 39.0, seconds = 60.0 } }
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ProviderOutage_Returns503()
        {
            using var app = new TestApp();
            app.Weather.Forecast = (_, _) => throw new WeatherUnavailableException("OpenWeather responded with 401");
            var client = await SignedInClientAsync(app);

            var response = await client.PostAsJsonAsync("/api/weather/route", Route());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.DoesNotContain("OpenWeather", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task RouteChecks_AreRateLimitedPerUser()
        {
            using var app = new TestApp(userActionsPerMinute: 2);
            var first = await SignedInClientAsync(app);
            var second = await SignedInClientAsync(app);

            await first.PostAsJsonAsync("/api/weather/route", Route());
            await first.PostAsJsonAsync("/api/weather/route", Route());

            Assert.Equal(HttpStatusCode.TooManyRequests, (await first.PostAsJsonAsync("/api/weather/route", Route())).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await second.PostAsJsonAsync("/api/weather/route", Route())).StatusCode);
        }
    }
}
