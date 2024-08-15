using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class DeleteUserDTO
    {
        [Required, DataType(DataType.EmailAddress), EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
