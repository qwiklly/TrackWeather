using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TrackWeatherWeb.Options;

namespace TrackWeatherWeb.Pages
{
    public class UsersMapModel(IOptions<YandexMapsOptions> options) : PageModel
    {
        public string ApiKey { get; } = options.Value.ApiKey;
    }
}
