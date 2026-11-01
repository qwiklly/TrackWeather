using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TrackWeatherWeb.DTOs;

namespace IntegrationTests
{
    public class AuthorizationTests(TestAppFixture fixture) : IClassFixture<TestAppFixture>
    {
        private readonly TestApp _app = fixture.App;

        [Theory]
        [InlineData("/api/users")]
        [InlineData("/api/users/root@example.com")]
        [InlineData("/api/transport-requests")]
        [InlineData("/api/weather?lat=51&lon=39")]
        public async Task Api_WithoutCredentials_Returns401(string url)
        {
            var response = await _app.CreateClient().GetAsync(url);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("DELETE", "/api/users/root@example.com")]
        [InlineData("POST", "/api/users")]
        [InlineData("PUT", "/api/users/root@example.com")]
        [InlineData("DELETE", "/api/transport-requests/1")]
        [InlineData("POST", "/api/transport-requests")]
        public async Task Mutations_WithoutCredentials_Return401(string method, string url)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), url)
            {
                Content = JsonContent.Create(new { })
            };

            var response = await _app.CreateClient().SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/UsersPage")]
        [InlineData("/AddUser")]
        [InlineData("/TransportRequestPage")]
        [InlineData("/AdminMap")]
        [InlineData("/UsersMap")]
        public async Task ProtectedPages_RedirectAnonymousVisitorsToLogin(string url)
        {
            var response = await _app.CreateBrowser().GetAsync(url);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/Login", response.Headers.Location!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("/UsersPage")]
        [InlineData("/AddUser")]
        [InlineData("/TransportRequestPage")]
        [InlineData("/AdminMap")]
        public async Task AdminPages_AreForbiddenForRegularUsers(string url)
        {
            var browser = await _app.SignInNewUserAsync();

            var response = await browser.GetAsync(url);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Error/403", response.Headers.Location!.OriginalString);
        }

        [Theory]
        [InlineData("/UsersPage")]
        [InlineData("/AddUser")]
        [InlineData("/TransportRequestPage")]
        [InlineData("/AdminMap")]
        [InlineData("/UsersMap")]
        public async Task Admin_CanOpenAllPages(string url)
        {
            var browser = await _app.SignInAdminAsync();

            var response = await browser.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PublicRegistration_CannotCreateAnAdmin()
        {
            var email = $"attacker-{Guid.NewGuid():N}@example.com";

            // The role field that used to be trusted is sent anyway
            var response = await _app.CreateClient().PostAsJsonAsync("/api/auth/register", new
            {
                email,
                name = "Attacker",
                password = TestApp.UserPassword,
                confirmPassword = TestApp.UserPassword,
                role = "Admin",
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var adminClient = _app.CreateClient();
            adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword));
            var created = await adminClient.GetFromJsonAsync<GetUsersDTO>($"/api/users/{email}");

            Assert.Equal("User", created!.Role);
        }

        [Fact]
        public async Task RegistrationForm_IgnoresTamperedRole()
        {
            var email = $"attacker-{Guid.NewGuid():N}@example.com";

            await _app.CreateBrowser().PostFormAsync("/Registration", new()
            {
                ["Register.Name"] = "Attacker",
                ["Register.Email"] = email,
                ["Register.Password"] = TestApp.UserPassword,
                ["Register.ConfirmPassword"] = TestApp.UserPassword,
                ["Register.Role"] = "Admin",
            });

            var browser = await _app.SignInBrowserAsync(email, TestApp.UserPassword);
            var response = await browser.GetAsync("/UsersPage");

            Assert.Contains("/Error/403", response.Headers.Location!.OriginalString);
        }

        [Fact]
        public async Task Jwt_GrantsApiAccess_AccordingToRole()
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            await _app.RegisterApiAsync(email);

            var userClient = _app.CreateClient();
            userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(email, TestApp.UserPassword));
            var adminClient = _app.CreateClient();
            adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword));

            Assert.Equal(HttpStatusCode.OK, (await userClient.GetAsync("/api/weather?lat=51&lon=39")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await userClient.GetAsync("/api/users")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/api/users")).StatusCode);
        }

        [Fact]
        public async Task ForgedJwt_IsRejected()
        {
            // Correct claims and issuer, but signed with a key the server does not know
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("attacker-controlled-signing-key-0123456789"));
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                issuer: "TrackWeather",
                audience: "TrackWeather.Api",
                claims: [new Claim(ClaimTypes.Email, "x@example.com"), new Claim(ClaimTypes.Role, "Admin")],
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));

            var client = _app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users")).StatusCode);
        }

        [Fact]
        public async Task ForgedCookie_IsRejected()
        {
            // The old build trusted any unsigned JWT placed in the AuthToken cookie
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims: [new Claim(ClaimTypes.Name, "x"), new Claim(ClaimTypes.Email, "x@example.com"), new Claim(ClaimTypes.Role, "Admin")]));

            var request = new HttpRequestMessage(HttpMethod.Get, "/UsersPage");
            request.Headers.Add("Cookie", $"AuthToken={token}; .TrackWeather.Auth={token}");

            var response = await _app.CreateBrowser().SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/Login", response.Headers.Location!.PathAndQuery, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UserApi_NeverReturnsPasswordHash()
        {
            var client = _app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword));

            var single = await client.GetStringAsync($"/api/users/{TestApp.AdminEmail}");
            var list = await client.GetStringAsync("/api/users");

            Assert.DoesNotContain("password", single, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", list, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("$2", single);
        }

        [Fact]
        public async Task Login_GivesTheSameAnswer_ForUnknownEmailAndWrongPassword()
        {
            var client = _app.CreateClient();

            var unknown = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO { Email = "nobody@example.com", Password = "whatever" });
            var wrong = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO { Email = TestApp.AdminEmail, Password = "whatever" });

            Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            Assert.Equal(
                (await unknown.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!.Detail,
                (await wrong.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!.Detail);
        }

        [Fact]
        public async Task LogoutViaGet_DoesNotSignOut()
        {
            var browser = await _app.SignInNewUserAsync();

            await browser.GetAsync("/LogoutPage");

            Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/UsersMap")).StatusCode);
        }

        [Fact]
        public async Task LogoutViaPost_SignsOut()
        {
            var browser = await _app.SignInNewUserAsync();

            await browser.PostFormAsync("/LogoutPage", [], tokenPage: "/UsersMap");

            Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/UsersMap")).StatusCode);
        }

        [Fact]
        public async Task Login_DoesNotRedirectToAnotherSite()
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            await _app.RegisterApiAsync(email);

            var response = await _app.CreateBrowser().PostFormAsync("/Login", new()
            {
                ["Login.Email"] = email,
                ["Login.Password"] = TestApp.UserPassword,
                ["ReturnUrl"] = "https://evil.example.com/",
            });

            Assert.DoesNotContain("evil.example.com", response.Headers.Location!.OriginalString);
        }
    }

    public sealed class TestAppFixture : IDisposable
    {
        public TestApp App { get; } = new();

        public void Dispose() => App.Dispose();
    }
}
