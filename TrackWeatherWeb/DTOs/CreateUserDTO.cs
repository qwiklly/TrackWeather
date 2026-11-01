using System.ComponentModel.DataAnnotations;
using TrackWeatherWeb.Auth;

namespace TrackWeatherWeb.DTOs
{
    // Only administrators can choose a role; public registration always creates a regular user
    public class CreateUserDTO : RegisterDTO
    {
        [Required(ErrorMessage = "Validation.Required")]
        [AllowedValues(Roles.User, Roles.Admin, ErrorMessage = "Validation.Role")]
        [Display(Name = "Field.Role")]
        public string Role { get; set; } = Roles.User;
    }
}
