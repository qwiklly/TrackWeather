using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.Services
{
    public enum TransportChangeKind
    {
        Created,
        Updated,
        Deleted
    }

    public sealed record TransportChange(
        int Id,
        [property: JsonConverter(typeof(JsonStringEnumConverter<TransportStatus>))] TransportStatus Status,
        [property: JsonConverter(typeof(JsonStringEnumConverter<TransportChangeKind>))] TransportChangeKind Kind);

    public interface ITransportNotifier
    {
        // Tells administrators and the listed users that a request changed
        Task NotifyAsync(TransportChange change, IEnumerable<string?> emails);
    }

    public class SignalRTransportNotifier(IHubContext<TransportHub> hub, ILogger<SignalRTransportNotifier> logger) : ITransportNotifier
    {
        public async Task NotifyAsync(TransportChange change, IEnumerable<string?> emails)
        {
            var groups = emails
                .Where(email => !string.IsNullOrEmpty(email))
                .Select(email => TransportHub.UserGroup(email!))
                .Append(TransportHub.AdminsGroup)
                .Distinct()
                .ToList();

            try
            {
                await hub.Clients.Groups(groups).SendAsync(TransportHub.ChangedEvent, change);
            }
            catch (Exception ex)
            {
                // The change is already saved; a missed live update must not turn it into an error
                logger.LogWarning(ex, "Could not send a live update for transport request {Id}", change.Id);
            }
        }
    }
}
