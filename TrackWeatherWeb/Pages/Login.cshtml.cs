﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.HttpServices;
using TrackWeatherWeb.States;

namespace TrackWeatherWeb.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        [BindProperty]
        public LoginDTO Login { get; set; } = new LoginDTO();

        public bool IsUserLoggedIn { get; set; } = false;

        private readonly IApplicationService _accountService;
        private readonly AuthenticationStateProvider _authStateProvider;

        public LoginModel(IApplicationService accountService, AuthenticationStateProvider authStateProvider)
        {
            _accountService = accountService;
            _authStateProvider = authStateProvider;
        }

        public void OnGet()
        {
            // Очищаем TempData после загрузки страницы
            TempData["ErrorMessage"] = null;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var response = await _accountService.LoginAsync(Login);

            if (!response.Flag)
            {
                TempData["ErrorMessage"] = response.Message;
                return Page();
            }

            var customAuthStateProvider = (AuthenticationProvider)_authStateProvider;
            await customAuthStateProvider.UpdateAuthenticationState(response.JWTToken);

            IsUserLoggedIn = true; 
            return Page(); 
        }
    }
}
