using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;

namespace TrackWeatherWeb.Pages
{
    [AllowAnonymous]
    [EnableRateLimiting(AuthDefaults.RateLimitPolicy)]
    public class RegistrationModel(IAccountRepo accountRepo) : PageModel
    {
        [BindProperty]
        public RegisterDTO Register { get; set; } = new RegisterDTO();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var response = await accountRepo.RegisterAsync(Register);
            if (!response.Flag)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return Page();
            }

            TempData["Success"] = response.Message;
            return RedirectToPage("/Login");
        }
    }
}
