using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Validation.Required"), EmailAddress(ErrorMessage = "Validation.Email")]
        [Display(Name = "Field.Email")]
        public string Email { get; set; } = string.Empty;

        // No format rules here: they belong to choosing a password, not to entering one
        [Required(ErrorMessage = "Validation.Required"), DataType(DataType.Password)]
        [Display(Name = "Field.Password")]
        public string Password { get; set; } = string.Empty;
    }
}
