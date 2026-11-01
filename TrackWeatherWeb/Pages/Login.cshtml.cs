using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;

namespace TrackWeatherWeb.Pages
{
    [AllowAnonymous]
    [EnableRateLimiting(AuthDefaults.RateLimitPolicy)]
    public class LoginModel(IAccountRepo accountRepo, IStringLocalizer<SharedResource> localizer) : PageModel
    {
        [BindProperty]
        public LoginDTO Login { get; set; } = new LoginDTO();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public IActionResult OnGet() =>
            User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await accountRepo.AuthenticateAsync(Login);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, localizer["Account.InvalidCredentials"]);
                return Page();
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserClaims.CreateCookiePrincipal(user));

            return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/UsersMap");
        }
    }
}
