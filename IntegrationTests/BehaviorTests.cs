using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories;

namespace IntegrationTests
{
    public class BehaviorTests(TestAppFixture fixture) : IClassFixture<TestAppFixture>
    {
        private readonly TestApp _app = fixture.App;

        private async Task<HttpClient> AdminApiAsync()
        {
            var client = _app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword));
            return client;
        }

        private async Task<HttpClient> UserApiAsync(string email)
        {
            await _app.RegisterApiAsync(email);
            var client = _app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(email, TestApp.UserPassword));
            return client;
        }

        [Fact]
        public async Task Health_IsPublic_AndChecksTheDatabase()
        {
            var response = await _app.CreateClient().GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SecurityHeaders_ArePresent()
        {
            var response = await _app.CreateClient().GetAsync("/");

            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
            Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        }

        [Fact]
        public async Task HomePage_DoesNotPublishCredentials()
        {
            var page = await _app.CreateClient().GetStringAsync("/");

            Assert.DoesNotContain("Admin123", page);
            Assert.DoesNotContain("admin@example.com", page);
        }

        [Fact]
        public async Task UnknownPage_ShowsFriendly404()
        {
            var browser = await _app.SignInNewUserAsync();

            var response = await browser.GetAsync("/no-such-page");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("error-card", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TransportRequest_TakesAuthorFromTheToken_NotFromTheBody()
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            var client = await UserApiAsync(email);

            var response = await client.PostAsJsonAsync("/api/transport-requests", new
            {
                latitude = 51.5m,
                longitude = 39.2m,
                comment = "Snow",
                email = "someone-else@example.com",
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var admin = await AdminApiAsync();
            var requests = await admin.GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests");

            Assert.Contains(requests!, r => r.Email == email && r.Comment == "Snow");
            Assert.DoesNotContain(requests!, r => r.Email == "someone-else@example.com");
        }

        [Theory]
        [InlineData(91, 0)]
        [InlineData(0, 181)]
        public async Task TransportRequest_RejectsImpossibleCoordinates(double latitude, double longitude)
        {
            var client = await UserApiAsync($"user-{Guid.NewGuid():N}@example.com");

            var response = await client.PostAsJsonAsync("/api/transport-requests", new { latitude, longitude, comment = "x" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Weather_RejectsImpossibleCoordinates()
        {
            var client = await UserApiAsync($"user-{Guid.NewGuid():N}@example.com");

            var response = await client.GetAsync("/api/weather?lat=120&lon=39");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Weather_WhenProviderFails_Returns503ProblemDetails()
        {
            using var app = new TestApp();
            app.Weather.Failure = new WeatherUnavailableException("OpenWeather responded with 401");
            var email = $"user-{Guid.NewGuid():N}@example.com";
            await app.RegisterApiAsync(email);
            var client = app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await app.GetJwtAsync(email, TestApp.UserPassword));

            var response = await client.GetAsync("/api/weather?lat=51&lon=39");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal(503, problem!.Status);
            Assert.DoesNotContain("OpenWeather", problem.Detail);
        }

        [Fact]
        public async Task DuplicateRegistration_Returns409_RegardlessOfCase()
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            await _app.RegisterApiAsync(email);

            var response = await _app.RegisterApiAsync(email.ToUpperInvariant());

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task WeakPassword_IsRejectedAtRegistration()
        {
            var response = await _app.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterDTO
            {
                Email = $"user-{Guid.NewGuid():N}@example.com",
                Name = "Weak",
                Password = "abcdefgh",
                ConfirmPassword = "abcdefgh",
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Admin_CannotDeleteThemselves()
        {
            var admin = await AdminApiAsync();

            var response = await admin.DeleteAsync($"/api/users/{TestApp.AdminEmail}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task LastAdmin_CannotBeDemoted()
        {
            using var app = new TestApp();
            var admin = app.CreateClient();
            admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword));

            var response = await admin.PutAsJsonAsync($"/api/users/{TestApp.AdminEmail}", new UpdateUserDTO { Role = "User" });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMissingUser_Returns404()
        {
            var admin = await AdminApiAsync();

            var response = await admin.DeleteAsync("/api/users/nobody@example.com");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task HostileUserName_CannotBreakOutOfTheDeleteConfirmation()
        {
            var hostileName = "x');alert(1);//<script>";
            await _app.RegisterApiAsync($"xss-{Guid.NewGuid():N}@example.com", hostileName);
            var admin = await _app.SignInAdminAsync();

            var page = await admin.GetStringAsync("/UsersPage");

            Assert.DoesNotContain("onsubmit", page);
            Assert.DoesNotContain("<script>alert", page);
            Assert.DoesNotContain(hostileName, page);
            Assert.Contains("data-confirm", page);
        }

        [Fact]
        public async Task LoginPage_IsRateLimited()
        {
            using var app = new TestApp(authPermitsPerMinute: 3);
            var client = app.CreateClient();

            HttpResponseMessage response = null!;
            for (var i = 0; i < 4; i++)
            {
                response = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO { Email = "a@example.com", Password = "x" });
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        [Fact]
        public async Task LoginForm_IsRateLimited()
        {
            using var app = new TestApp(authPermitsPerMinute: 2);
            var browser = app.CreateBrowser();

            await browser.GetAsync("/Login");
            await browser.GetAsync("/Login");

            Assert.Equal(HttpStatusCode.TooManyRequests, (await browser.GetAsync("/Login")).StatusCode);
        }

        [Fact]
        public async Task AddUserForm_CreatesAdmin_AndDeleteForm_RemovesThem()
        {
            var email = $"new-admin-{Guid.NewGuid():N}@example.com";
            var browser = await _app.SignInAdminAsync();

            var created = await browser.PostFormAsync("/AddUser", new()
            {
                ["Register.Role"] = "Admin",
                ["Register.Name"] = "New Admin",
                ["Register.Email"] = email,
                ["Register.Password"] = TestApp.UserPassword,
                ["Register.ConfirmPassword"] = TestApp.UserPassword,
            });

            Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
            Assert.Contains(email, await browser.GetStringAsync("/UsersPage"));

            var deleted = await browser.PostFormAsync($"/UsersPage?handler=Delete&email={email}", [], tokenPage: "/UsersPage");

            Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
            Assert.DoesNotContain(email, await browser.GetStringAsync("/UsersPage"));
        }

        [Fact]
        public async Task TransportRequestsPage_ListsAndDeletesRequests()
        {
            var client = await UserApiAsync($"user-{Guid.NewGuid():N}@example.com");
            var comment = $"Ice {Guid.NewGuid():N}";
            await client.PostAsJsonAsync("/api/transport-requests", new { latitude = 51.5m, longitude = 39.2m, comment });

            var requests = await (await AdminApiAsync()).GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests");
            var id = requests!.Single(r => r.Comment == comment).Id;
            var browser = await _app.SignInAdminAsync();
            Assert.Contains(comment, await browser.GetStringAsync("/TransportRequestPage"));

            await browser.PostFormAsync($"/TransportRequestPage?handler=Delete&id={id}", [], tokenPage: "/TransportRequestPage");

            Assert.DoesNotContain(comment, await browser.GetStringAsync("/TransportRequestPage"));
        }

        [Fact]
        public async Task PasswordChange_TakesEffect()
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            await _app.RegisterApiAsync(email);
            var admin = await AdminApiAsync();

            var response = await admin.PutAsJsonAsync($"/api/users/{email}", new UpdateUserDTO { Password = "N3wPassword" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(await _app.GetJwtAsync(email, "N3wPassword"));
        }
    }

    public class DatabaseInitializerTests
    {
        [Fact]
        public async Task PublishedDefaultPassword_IsLockedOutsideDevelopment()
        {
            using var app = new LegacyAdminApp();
            var client = app.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO { Email = "admin@example.com", Password = "Admin123" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private sealed class LegacyAdminApp : TestApp
        {
            protected override void SeedDatabase(TrackWeatherWeb.Data.AppDbContext db)
            {
                db.Users.Add(new TrackWeatherWeb.Models.ApplicationUsers
                {
                    Name = "Admin",
                    Email = "admin@example.com",
                    Role = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123")
                });
                db.SaveChanges();
            }
        }
    }
}
