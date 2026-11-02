using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class AssignTransportRequestDTO
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}
