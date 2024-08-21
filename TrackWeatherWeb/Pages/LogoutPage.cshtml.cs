using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrackWeatherWeb.HttpServices;
using TrackWeatherWeb.States;

namespace TrackWeatherWeb.Pages
{
    public class LogoutPageModel : PageModel
    {
		private readonly AuthenticationStateProvider _authStateProvider;

		public LogoutPageModel(IApplicationService accountService, AuthenticationStateProvider authStateProvider)
		{
			_authStateProvider = authStateProvider;
		}

        public async Task<IActionResult> OnGet()
        {
            var customAuthStateProvider = (AuthenticationProvider)_authStateProvider;
            await customAuthStateProvider.UpdateAuthenticationState(null);
            return RedirectToPage("/Login");
        }	
	}
}
