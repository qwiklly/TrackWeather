using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class RegisterDTO : LoginDTO
    {

        [Required, Compare(nameof(Password)), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = string.Empty;

    }
}
