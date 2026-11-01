using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TrackWeatherWeb.Pages
{
    public class LogoutPageModel : PageModel
    {
        // Signing out changes state, so it is POST only: a link on another site must not log the user out
        public IActionResult OnGet() => RedirectToPage("/Index");

        public async Task<IActionResult> OnPostAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }
    }
}
