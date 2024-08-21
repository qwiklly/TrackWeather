using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.HttpServices;

namespace TrackWeatherWeb.Pages
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public RegisterDTO Register { get; set; } = new RegisterDTO();

        public bool IsUserAdded { get; set; } = false;

        private readonly IApplicationService _accountService;

        public RegisterModel(IApplicationService accountService)
        {
            _accountService = accountService;
        }

        public void OnGet()
        {
            // Очищаем TempData после загрузки страницы
            TempData["SuccessMessage"] = null;
            TempData["ErrorMessage"] = null;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var response = await _accountService.RegisterAsync(Register);

            if (!response.Flag)
            {
                TempData["ErrorMessage"] = response.Message;
                return Page();
            }

            IsUserAdded = true;  
            return Page(); 
        }
    }
}
