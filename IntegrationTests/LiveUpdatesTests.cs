using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using TrackWeatherWeb.DTOs;

namespace IntegrationTests
{
    public class LiveUpdatesTests
    {
        private sealed class Listener : IAsyncDisposable
        {
            private readonly HubConnection _connection;
            private readonly List<JsonElement> _events = [];
            private readonly SemaphoreSlim _signal = new(0);

            public Listener(HubConnection connection)
            {
                _connection = connection;
                _connection.On<JsonElement>("requestChanged", change =>
                {
                    lock (_events)
                    {
                        _events.Add(change);
                    }
                    _signal.Release();
                });
            }

            public int Count { get { lock (_events) { return _events.Count; } } }

            public Task StartAsync() => _connection.StartAsync();

            public async Task<JsonElement> NextAsync()
            {
                Assert.True(await _signal.WaitAsync(TimeSpan.FromSeconds(10)), "No live update arrived");
                lock (_events)
                {
                    return _events[^1];
                }
            }

            public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
        }

        private static HubConnection Connect(TestApp app, string? token) =>
            new HubConnectionBuilder()
                .WithUrl(new Uri(app.Server.BaseAddress, "hubs/transport"), options =>
                {
                    options.HttpMessageHandlerFactory = _ => app.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                    if (token != null)
                    {
                        options.Headers["Authorization"] = $"Bearer {token}";
                    }
                })
                .Build();

        private static async Task<Listener> ListenAsync(TestApp app, string token)
        {
            var listener = new Listener(Connect(app, token));
            await listener.StartAsync();
            return listener;
        }

        private static async Task<(HttpClient Client, string Email, string Token)> NewUserAsync(TestApp app)
        {
            var email = $"user-{Guid.NewGuid():N}@example.com";
            await app.RegisterApiAsync(email);
            var token = await app.GetJwtAsync(email, TestApp.UserPassword);
            var client = app.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, email, token);
        }

        private static async Task<int> CreateRequestAsync(HttpClient author)
        {
            var comment = Guid.NewGuid().ToString("N");
            await author.PostAsJsonAsync("/api/transport-requests", new { latitude = 51.5m, longitude = 39.2m, comment });
            var mine = await author.GetFromJsonAsync<List<RequestTransportDTO>>("/api/transport-requests/mine");
            return mine!.Single(r => r.Comment == comment).Id;
        }

        [Fact]
        public async Task AnonymousVisitor_CannotConnect()
        {
            using var app = new TestApp();
            _ = app.CreateClient();
            await using var connection = Connect(app, null);

            await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync());
        }

        [Fact]
        public async Task Dispatcher_SeesNewRequests_Immediately()
        {
            using var app = new TestApp();
            var (author, _, _) = await NewUserAsync(app);
            var adminToken = await app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword);
            await using var dispatcher = await ListenAsync(app, adminToken);

            var id = await CreateRequestAsync(author);

            var change = await dispatcher.NextAsync();
            Assert.Equal(id, change.GetProperty("id").GetInt32());
            Assert.Equal("New", change.GetProperty("status").GetString());
            Assert.Equal("Created", change.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task AuthorAndAssignee_AreTold_AndOutsidersAreNot()
        {
            using var app = new TestApp();
            var (authorClient, _, authorToken) = await NewUserAsync(app);
            var (_, executorEmail, executorToken) = await NewUserAsync(app);
            var (_, _, outsiderToken) = await NewUserAsync(app);
            var admin = app.CreateClient();
            admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword));

            var id = await CreateRequestAsync(authorClient);

            await using var author = await ListenAsync(app, authorToken);
            await using var executor = await ListenAsync(app, executorToken);
            await using var outsider = await ListenAsync(app, outsiderToken);

            await admin.PostAsJsonAsync($"/api/transport-requests/{id}/assign", new { email = executorEmail });

            var toAuthor = await author.NextAsync();
            var toExecutor = await executor.NextAsync();
            Assert.Equal("InProgress", toAuthor.GetProperty("status").GetString());
            Assert.Equal(id, toExecutor.GetProperty("id").GetInt32());

            await Task.Delay(300);
            Assert.Equal(0, outsider.Count);
        }

        [Fact]
        public async Task Administrator_GetsOneEvent_EvenForTheirOwnRequest()
        {
            using var app = new TestApp();
            var adminToken = await app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword);
            var admin = app.CreateClient();
            admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            await using var listener = await ListenAsync(app, adminToken);

            await CreateRequestAsync(admin);

            await listener.NextAsync();
            await Task.Delay(300);
            Assert.Equal(1, listener.Count);
        }

        [Fact]
        public async Task LiveEvents_CarryNoPersonalData()
        {
            using var app = new TestApp();
            var (author, email, _) = await NewUserAsync(app);
            var adminToken = await app.GetJwtAsync(TestApp.AdminEmail, TestApp.AdminPassword);
            await using var dispatcher = await ListenAsync(app, adminToken);

            await CreateRequestAsync(author);

            var change = await dispatcher.NextAsync();
            Assert.DoesNotContain(email, change.GetRawText());
        }
    }
}
