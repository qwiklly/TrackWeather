using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;

namespace TrackWeatherWeb.Pages
{
    [Authorize(Roles = Roles.Admin)]
    public class UsersPageModel(IAccountRepo accountRepo) : PageModel
    {
        public List<GetUsersDTO> Users { get; set; } = [];

        public async Task OnGetAsync()
        {
            Users = await accountRepo.GetUsersAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string email)
        {
            var response = await accountRepo.DeleteUserAsync(email, User.FindFirstValue(ClaimTypes.Email));
            TempData[response.Flag ? "Success" : "Error"] = response.Message;
            return RedirectToPage();
        }
    }
}
