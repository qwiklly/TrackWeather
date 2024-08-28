using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TrackWeatherWeb.Pages
{
    public class IndexModel(AuthenticationStateProvider authStateProvider) : PageModel
    {
        private readonly AuthenticationStateProvider _authStateProvider = authStateProvider;
        public string? UserName { get; private set; }

        public async Task OnGet()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // Если пользователь авторизован, берем его имя
            if (user.Identity!.IsAuthenticated)
            {
                UserName = user.Identity.Name!;
            }
        }
    }
}
