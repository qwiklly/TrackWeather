using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class RequestTransportDTO
    {
        [Required, DataType(DataType.EmailAddress), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public decimal? Coordinate_x { get; set; }

        [Required]
        public decimal? Coordinate_y { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        public int? Id { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
