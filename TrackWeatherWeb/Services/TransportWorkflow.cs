using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.Services
{
    // Who is acting on a transport request
    public sealed record TransportActor(string Email, bool IsAdmin);

    // New -> InProgress (assigned by an administrator) -> Completed; New and InProgress can also be cancelled
    public static class TransportWorkflow
    {
        public static bool IsOpen(TransportStatus status) =>
            status is TransportStatus.New or TransportStatus.InProgress;

        public static bool CanAssign(TransportStatus status, TransportActor actor) =>
            actor.IsAdmin && IsOpen(status);

        public static bool CanComplete(TransportStatus status, string? assignee, TransportActor actor) =>
            status == TransportStatus.InProgress && (actor.IsAdmin || assignee == actor.Email);

        // An author may withdraw a request only until someone has taken it
        public static bool CanCancel(TransportStatus status, string author, TransportActor actor) =>
            IsOpen(status) && (actor.IsAdmin || (status == TransportStatus.New && author == actor.Email));
    }
}
