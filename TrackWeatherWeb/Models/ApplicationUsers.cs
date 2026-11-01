using System.ComponentModel.DataAnnotations;

namespace TrackWeatherWeb.Models
{
    public class ApplicationUsers
    {
        public int Id { get; set; }
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
        [MaxLength(32)]
        public string Role { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
