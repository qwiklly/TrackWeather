using System.ComponentModel.DataAnnotations;
using TrackWeatherWeb.Auth;

namespace TrackWeatherWeb.DTOs
{
    // Null fields stay unchanged
    public class UpdateUserDTO
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [AllowedValues(Roles.User, Roles.Admin, null, ErrorMessage = "Validation.Role")]
        public string? Role { get; set; }

        [RegularExpression(RegisterDTO.PasswordPattern, ErrorMessage = "Validation.Password")]
        public string? Password { get; set; }
    }
}
