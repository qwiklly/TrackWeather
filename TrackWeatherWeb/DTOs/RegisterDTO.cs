using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.DTOs
{
    public class RegisterDTO
    {
        public const string PasswordPattern = "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9]).{8,}$";

        [Required(ErrorMessage = "Validation.Required"), EmailAddress(ErrorMessage = "Validation.Email"), MaxLength(256, ErrorMessage = "Validation.MaxLength")]
        [Display(Name = "Field.Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.Required"), DataType(DataType.Password)]
        [RegularExpression(PasswordPattern, ErrorMessage = "Validation.Password")]
        [Display(Name = "Field.Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.Required"), DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Validation.PasswordsDiffer")]
        [Display(Name = "Field.ConfirmPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.Required"), MaxLength(100, ErrorMessage = "Validation.MaxLength")]
        [Display(Name = "Field.Name")]
        public string Name { get; set; } = string.Empty;
    }
}
