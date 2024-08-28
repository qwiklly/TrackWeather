using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TrackWeatherWeb.Pages
{
    public class AdminMapModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string? ApiKey { get; private set; }

        public AdminMapModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            ApiKey = _configuration["YandexMaps:ApiKey"]!;
        }
    }
}
