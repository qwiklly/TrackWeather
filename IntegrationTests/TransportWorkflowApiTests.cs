using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;

namespace IntegrationTests
{
    public class TransportWorkflowApiTests(TestAppFixture fixture) : IClassFixture<TestAppFixture>
    {
        private readonly TestApp _app = fixture.App;

        private async Task<(HttpClient Client, string Email)> NewUserAsync()
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            await _app.RegisterApiAsync(email);
            var client = _app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(email, TestApp.UserPassword));
            return (client, email);
        }

        private async Task<HttpClient> AdminAsync()
        {
            var client = _app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword));
            return client;
        }

        // Creates a request and returns it, found through the author's own list
        private static async Task<RequestTransportDTO> CreateRequestAsync(HttpClient author, string? comment = null)
        {
            comment ??= $"Stuck {Guid.NewGuid():N}";
            var response = await author.PostAsJsonAsync("/api/transport-requests", new { latitude = 51.5m, longitude = 39.2m, comment });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var mine = await author.GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests/mine");
            return mine!.Single(r => r.Comment == comment);
        }

        private static Task<HttpResponseMessage> AssignAsync(HttpClient client, int id, string email) =>
            client.PostAsJsonAsync($"/api/transport-requests/{id}/assign", new { email });

        private static async Task<RequestTransportDTO> ReadAsync(HttpClient admin, int id) =>
            (await admin.GetFromJsonAsync<RequestTransportDTO>($"/api/transport-requests/{id}"))!;

        [Fact]
        public async Task FullLifecycle_FromNewToCompleted()
        {
            var (author, _) = await NewUserAsync();
            var (executor, executorEmail) = await NewUserAsync();
            var admin = await AdminAsync();

            var request = await CreateRequestAsync(author);
            Assert.Equal(TransportStatus.New, request.Status);
            Assert.Null(request.AssignedToEmail);

            Assert.Equal(HttpStatusCode.OK, (await AssignAsync(admin, request.Id, executorEmail)).StatusCode);
            var assigned = await ReadAsync(admin, request.Id);
            Assert.Equal(TransportStatus.InProgress, assigned.Status);
            Assert.Equal(executorEmail, assigned.AssignedToEmail);
            Assert.NotNull(assigned.UpdatedAt);

            var mine = await executor.GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests/mine");
            Assert.Contains(mine!, r => r.Id == request.Id);

            Assert.Equal(HttpStatusCode.OK, (await executor.PostAsync($"/api/transport-requests/{request.Id}/complete", null)).StatusCode);
            Assert.Equal(TransportStatus.Completed, (await ReadAsync(admin, request.Id)).Status);
        }

        [Fact]
        public async Task Author_CannotSetStatusOrAssignee_WhenCreating()
        {
            var (author, email) = await NewUserAsync();
            var comment = $"Sneaky {Guid.NewGuid():N}";

            await author.PostAsJsonAsync("/api/transport-requests", new
            {
                latitude = 51.5m, longitude = 39.2m, comment,
                status = "Completed", assignedToEmail = "friend@example.com", email = "someone@example.com"
            });

            var request = (await author.GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests/mine"))!.Single(r => r.Comment == comment);
            Assert.Equal(TransportStatus.New, request.Status);
            Assert.Null(request.AssignedToEmail);
            Assert.Equal(email, request.Email);
        }

        [Fact]
        public async Task RegularUser_CannotAssign()
        {
            var (author, email) = await NewUserAsync();
            var request = await CreateRequestAsync(author);

            var response = await AssignAsync(author, request.Id, email);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OnlyTheAssigneeOrAdmin_CanComplete()
        {
            var (author, _) = await NewUserAsync();
            var (_, executorEmail) = await NewUserAsync();
            var (stranger, _) = await NewUserAsync();
            var admin = await AdminAsync();
            var request = await CreateRequestAsync(author);
            await AssignAsync(admin, request.Id, executorEmail);

            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsync($"/api/transport-requests/{request.Id}/complete", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await author.PostAsync($"/api/transport-requests/{request.Id}/complete", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/transport-requests/{request.Id}/complete", null)).StatusCode);
        }

        [Fact]
        public async Task Author_CanCancel_UntilTheRequestIsTaken()
        {
            var (author, _) = await NewUserAsync();
            var (_, executorEmail) = await NewUserAsync();
            var admin = await AdminAsync();
            var open = await CreateRequestAsync(author);
            var taken = await CreateRequestAsync(author);
            await AssignAsync(admin, taken.Id, executorEmail);

            Assert.Equal(HttpStatusCode.OK, (await author.PostAsync($"/api/transport-requests/{open.Id}/cancel", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await author.PostAsync($"/api/transport-requests/{taken.Id}/cancel", null)).StatusCode);
            Assert.Equal(TransportStatus.Cancelled, (await ReadAsync(admin, open.Id)).Status);
        }

        [Fact]
        public async Task Stranger_CannotCancel()
        {
            var (author, _) = await NewUserAsync();
            var (stranger, _) = await NewUserAsync();
            var request = await CreateRequestAsync(author);

            var response = await stranger.PostAsync($"/api/transport-requests/{request.Id}/cancel", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task FinishedRequest_CannotChange()
        {
            var (author, email) = await NewUserAsync();
            var admin = await AdminAsync();
            var request = await CreateRequestAsync(author);
            await admin.PostAsync($"/api/transport-requests/{request.Id}/cancel", null);

            Assert.Equal(HttpStatusCode.Conflict, (await AssignAsync(admin, request.Id, email)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/transport-requests/{request.Id}/complete", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/transport-requests/{request.Id}/cancel", null)).StatusCode);
        }

        [Fact]
        public async Task NewRequest_CannotBeCompleted()
        {
            var (author, _) = await NewUserAsync();
            var admin = await AdminAsync();
            var request = await CreateRequestAsync(author);

            var response = await admin.PostAsync($"/api/transport-requests/{request.Id}/complete", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Assign_RejectsUnknownUser_AndMissingRequest()
        {
            var (author, email) = await NewUserAsync();
            var admin = await AdminAsync();
            var request = await CreateRequestAsync(author);

            Assert.Equal(HttpStatusCode.NotFound, (await AssignAsync(admin, request.Id, "nobody@example.com")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await AssignAsync(admin, 999999, email)).StatusCode);
            Assert.Equal(TransportStatus.New, (await ReadAsync(admin, request.Id)).Status);
        }

        [Fact]
        public async Task Reassigning_KeepsTheRequestInProgress()
        {
            var (author, _) = await NewUserAsync();
            var (first, firstEmail) = await NewUserAsync();
            var (_, secondEmail) = await NewUserAsync();
            var admin = await AdminAsync();
            var request = await CreateRequestAsync(author);
            await AssignAsync(admin, request.Id, firstEmail);

            await AssignAsync(admin, request.Id, secondEmail);

            var updated = await ReadAsync(admin, request.Id);
            Assert.Equal(TransportStatus.InProgress, updated.Status);
            Assert.Equal(secondEmail, updated.AssignedToEmail);
            Assert.Equal(HttpStatusCode.Forbidden, (await first.PostAsync($"/api/transport-requests/{request.Id}/complete", null)).StatusCode);
        }

        [Fact]
        public async Task Mine_ShowsOnlyOwnAndAssignedRequests()
        {
            var (author, _) = await NewUserAsync();
            var (other, _) = await NewUserAsync();
            var request = await CreateRequestAsync(author);

            var theirs = await other.GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests/mine");

            Assert.DoesNotContain(theirs!, r => r.Id == request.Id);
        }

        [Fact]
        public async Task List_FiltersByStatus_AndRejectsUnknownStatus()
        {
            var (author, _) = await NewUserAsync();
            var admin = await AdminAsync();
            var request = await CreateRequestAsync(author);
            await admin.PostAsync($"/api/transport-requests/{request.Id}/cancel", null);

            var cancelled = await admin.GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests?status=Cancelled");
            var fresh = await admin.GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests?status=New");

            Assert.Contains(cancelled!, r => r.Id == request.Id);
            Assert.DoesNotContain(fresh!, r => r.Id == request.Id);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/transport-requests?status=Bogus")).StatusCode);
        }

        [Fact]
        public async Task Update_KeepsTheCreationTime()
        {
            var (author, _) = await NewUserAsync();
            var admin = await AdminAsync();
            var request = await CreateRequestAsync(author);

            await admin.PutAsJsonAsync($"/api/transport-requests/{request.Id}", new { latitude = 52m, longitude = 40m, comment = "Moved" });

            var updated = await ReadAsync(admin, request.Id);
            Assert.Equal(52m, updated.Latitude);
            Assert.Equal(request.Timestamp, updated.Timestamp);
            Assert.NotNull(updated.UpdatedAt);
        }

        [Fact]
        public async Task StaleWrite_IsRejected_WhenSomeoneChangedTheStatusFirst()
        {
            var (author, _) = await NewUserAsync();
            var request = await CreateRequestAsync(author);

            using var firstScope = _app.Services.CreateScope();
            using var secondScope = _app.Services.CreateScope();
            var first = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var second = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var firstCopy = await first.Requests.FindAsync(request.Id);
            var secondCopy = await second.Requests.FindAsync(request.Id);

            secondCopy!.Status = TransportStatus.Cancelled;
            await second.SaveChangesAsync();

            firstCopy!.Status = TransportStatus.InProgress;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => first.SaveChangesAsync());
        }

        [Fact]
        public async Task AdminPage_AssignsCompletes_AndOffersOnlyValidActions()
        {
            var (author, _) = await NewUserAsync();
            var (_, executorEmail) = await NewUserAsync();
            var request = await CreateRequestAsync(author);
            var browser = await _app.SignInAdminAsync();

            var assigned = await browser.PostFormAsync(
                $"/TransportRequestPage?handler=Assign&id={request.Id}",
                new() { ["email"] = executorEmail },
                tokenPage: "/TransportRequestPage");
            Assert.Equal(HttpStatusCode.Redirect, assigned.StatusCode);

            var table = await browser.GetStringAsync("/TransportRequestPage?handler=Table");
            Assert.DoesNotContain("<html", table);
            Assert.Contains("status-badge--inprogress", table);
            Assert.Contains("handler=Complete", table);

            await browser.PostFormAsync($"/TransportRequestPage?handler=Complete&id={request.Id}", [], tokenPage: "/TransportRequestPage");

            var finished = await browser.GetStringAsync("/TransportRequestPage?status=Completed");
            Assert.Contains(request.Comment, finished);
            Assert.DoesNotContain("handler=Assign", finished);
        }

        [Fact]
        public async Task TablePartial_IsForAdminsOnly()
        {
            var browser = await _app.SignInNewUserAsync();

            var response = await browser.GetAsync("/TransportRequestPage?handler=Table");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Error/403", response.Headers.Location!.OriginalString);
        }

        [Fact]
        public async Task MyRequestsPage_ShowsRequests_AndLetsTheAuthorCancel()
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            var browser = await _app.SignInNewUserAsync(email);
            var client = _app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _app.GetJwtAsync(email, TestApp.UserPassword));
            var request = await CreateRequestAsync(client);

            var page = await browser.GetStringAsync("/MyRequests");
            Assert.Contains(request.Comment, page);
            Assert.Contains("data-live-table", page);

            await browser.PostFormAsync($"/MyRequests?handler=Cancel&id={request.Id}", [], tokenPage: "/MyRequests");

            Assert.Contains("status-badge--cancelled", await browser.GetStringAsync("/MyRequests?handler=Table"));
        }

        [Fact]
        public async Task MyRequestsPage_RequiresSignIn()
        {
            var response = await _app.CreateBrowser().GetAsync("/MyRequests");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task HostileComment_IsEncodedInTheTable()
        {
            var (author, _) = await NewUserAsync();
            await CreateRequestAsync(author, "<script>alert(1)</script>");
            var browser = await _app.SignInAdminAsync();

            var table = await browser.GetStringAsync("/TransportRequestPage?handler=Table");

            Assert.DoesNotContain("<script>alert(1)", table);
            Assert.Contains("&lt;script&gt;alert(1)", table);
        }
    }
}
