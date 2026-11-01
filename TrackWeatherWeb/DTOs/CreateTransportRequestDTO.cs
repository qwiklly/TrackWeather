using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class CreateTransportRequestDTO
    {
        [Required, Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Required, Range(-180, 180)]
        public decimal? Longitude { get; set; }

        [Required, MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }
}
