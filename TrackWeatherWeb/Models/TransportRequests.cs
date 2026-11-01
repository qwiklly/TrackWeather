using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackWeatherWeb.Models
{
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
    }
}
