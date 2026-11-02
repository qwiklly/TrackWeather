using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TrackWeatherWeb.Auth;

namespace TrackWeatherWeb.Services
{
    // Server to client only: browsers listen for changes to the requests that concern them
    [Authorize]
    public class TransportHub : Hub
    {
        public const string Path = "/hubs/transport";
        public const string ChangedEvent = "requestChanged";
        public const string AdminsGroup = "admins";

        public static string UserGroup(string email) => $"user:{email}";

        public override async Task OnConnectedAsync()
        {
            var user = Context.User!;
            var email = user.FindFirstValue(ClaimTypes.Email);
            // One group per connection: administrators get everything, and a connection
            // in two groups would receive an event twice when both are addressed
            if (user.IsInRole(Roles.Admin))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
            }
            else if (email != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(email));
            }
            await base.OnConnectedAsync();
        }
    }
}
