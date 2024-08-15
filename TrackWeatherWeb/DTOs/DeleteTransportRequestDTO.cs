using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class DeleteTransportRequestDTO
    {
        [Required]
        public int Id { get; set; }
    }
}
