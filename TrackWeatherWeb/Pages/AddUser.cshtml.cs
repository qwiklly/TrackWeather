using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;

namespace TrackWeatherWeb.Pages
{
    [Authorize(Roles = Roles.Admin)]
    public class AddUserModel(IAccountRepo accountRepo) : PageModel
    {
        [BindProperty]
        public CreateUserDTO Register { get; set; } = new CreateUserDTO();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var response = await accountRepo.CreateUserAsync(Register);
            if (!response.Flag)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return Page();
            }

            TempData["Success"] = response.Message;
            return RedirectToPage("/UsersPage");
        }
    }
}
