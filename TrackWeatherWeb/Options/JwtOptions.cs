using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.Options
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        // HMAC-SHA256 needs at least 256 bits
        [Required(ErrorMessage = "Jwt:Key is not configured. Set the Jwt__Key environment variable.")]
        [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters long.")]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int ExpiryMinutes { get; set; } = 60;
    }
}
