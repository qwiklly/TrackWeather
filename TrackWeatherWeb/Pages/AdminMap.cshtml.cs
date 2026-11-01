using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.Options;

namespace TrackWeatherWeb.Pages
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminMapModel(IOptions<YandexMapsOptions> options) : PageModel
    {
        public string ApiKey { get; } = options.Value.ApiKey;
    }
}
