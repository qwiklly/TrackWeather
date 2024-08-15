using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class GetUsersDTO
    {

        [Required, DataType(DataType.EmailAddress), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
