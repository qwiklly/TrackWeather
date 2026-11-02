using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.DTOs
{
    public class RequestTransportDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TransportStatus Status { get; set; }
        public string? AssignedToEmail { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
