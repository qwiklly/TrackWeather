using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;
using TrackWeatherWeb.WeatherModels;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace IntegrationTests
{
    // The whole application in memory: real pipeline, real authorization, SQLite instead of SQL Server
    public partial class TestApp : WebApplicationFactory<Program>
    {
        public const string AdminEmail = "root@example.com";
        public const string AdminPassword = "Adm1nistrator";
        public const string UserPassword = "Passw0rd1";

        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly int _authPermitsPerMinute;
        private readonly int _userActionsPerMinute;
        private readonly string _environment;
        private readonly string _jwtKey;

        public FakeWeatherRepo Weather { get; } = new();

        public TestApp(
            int authPermitsPerMinute = 10_000,
            int userActionsPerMinute = 10_000,
            string environment = "Testing",
            string jwtKey = "integration-tests-only-signing-key-0123456789")
        {
            _environment = environment;
            _jwtKey = jwtKey;
            _authPermitsPerMinute = authPermitsPerMinute;
            _userActionsPerMinute = userActionsPerMinute;
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = _jwtKey,
                ["Jwt:Issuer"] = "TrackWeather",
                ["Jwt:Audience"] = "TrackWeather.Api",
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
                ["RateLimiting:AuthPermitsPerMinute"] = _authPermitsPerMinute.ToString(),
                ["RateLimiting:UserActionsPerMinute"] = _userActionsPerMinute.ToString(),
                ["YandexMaps:ApiKey"] = "test-yandex-key",
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IWeatherRepo>();
                services.AddSingleton<IWeatherRepo>(Weather);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                SeedDatabase(db);
            });
        }

        protected virtual void SeedDatabase(AppDbContext db)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection.Dispose();
        }

        // A browser-like client: keeps cookies and does not follow redirects, so tests can assert them
        public HttpClient CreateBrowser(string? language = null)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
            if (language != null)
            {
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
            }
            return client;
        }

        public async Task<HttpClient> SignInBrowserAsync(string email, string password)
        {
            var browser = CreateBrowser();
            var response = await browser.PostFormAsync("/Login", new()
            {
                ["Login.Email"] = email,
                ["Login.Password"] = password,
            });
            Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
            return browser;
        }

        public Task<HttpClient> SignInAdminAsync() => SignInBrowserAsync(AdminEmail, AdminPassword);

        public async Task<HttpClient> SignInNewUserAsync(string? email = null)
        {
            email ??= $"user-{Guid.NewGuid():N}@example.com";
            await RegisterApiAsync(email);
            return await SignInBrowserAsync(email, UserPassword);
        }

        public Task<HttpResponseMessage> RegisterApiAsync(string email, string name = "Test User") =>
            CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterDTO
            {
                Email = email,
                Name = name,
                Password = UserPassword,
                ConfirmPassword = UserPassword,
            });

        public async Task<string> GetJwtAsync(string email, string password)
        {
            var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new LoginDTO { Email = email, Password = password });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<LoginResponse>())!.JWTToken;
        }
    }

    public sealed class FakeWeatherRepo : IWeatherRepo
    {
        public string? LastLanguage { get; private set; }

        public Exception? Failure { get; set; }

        public Func<double, double, IReadOnlyList<ForecastItem>> Forecast { get; set; } = (_, _) => [];

        public Task<IReadOnlyList<ForecastItem>> GetForecastAsync(double lat, double lon, string language)
        {
            LastLanguage = language;
            return Task.FromResult(Forecast(lat, lon));
        }

        public Task<WeatherDTO> GetCurrentWeatherAsync(double lat, double lon, string language)
        {
            LastLanguage = language;
            return Failure != null
                ? Task.FromException<WeatherDTO>(Failure)
                : Task.FromResult(new WeatherDTO { City = "Test City", Temp = 21.5, Summary = "clear sky" });
        }
    }

    public static partial class HttpClientExtensions
    {
        // Loads the page first, like a browser would, to pick up the antiforgery token
        public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string url, Dictionary<string, string> fields, string? tokenPage = null)
        {
            var page = await client.GetStringAsync(tokenPage ?? url);
            var token = AntiforgeryToken().Match(page).Groups[1].Value;
            Assert.False(string.IsNullOrEmpty(token), $"No antiforgery token on {tokenPage ?? url}");

            fields["__RequestVerificationToken"] = token;
            return await client.PostAsync(url, new FormUrlEncodedContent(fields));
        }

        [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
        private static partial Regex AntiforgeryToken();
    }
}
