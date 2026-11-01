using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace TrackWeatherWeb.Controllers
{
    // Form target of the language switcher in the page header
    [Route("language")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LanguageController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Set([FromForm] string culture, [FromForm] string? returnUrl)
        {
            if (SupportedLanguages.IsSupported(culture))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture.ToLowerInvariant())),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = Request.IsHttps
                    });
            }

            // Url.IsLocalUrl keeps this from becoming an open redirect
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "~/");
        }
    }
}
