using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TrackWeatherWeb.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter<TransportStatus>))]
    public enum TransportStatus
    {
        New,
        InProgress,
        Completed,
        Cancelled
    }

    public class TransportRequests
    {
        public int Id { get; set; }
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 6)")]
        public decimal Latitude { get; set; }
        [Column(TypeName = "decimal(18, 6)")]
        public decimal Longitude { get; set; }
        [MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        // Concurrency token: two dispatchers acting on one request cannot both succeed
        [ConcurrencyCheck]
        public TransportStatus Status { get; set; } = TransportStatus.New;
        [MaxLength(256)]
        public string? AssignedToEmail { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
