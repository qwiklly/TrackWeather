using System.ComponentModel.DataAnnotations.Schema;

namespace TrackWeatherWeb.Models
{
    public class TransportRequests
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18, 6)")]
        public decimal? Coordinate_x { get; set; }
        [Column(TypeName = "decimal(18, 6)")]
        public decimal? Coordinate_y { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
