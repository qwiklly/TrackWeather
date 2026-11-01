using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.Auth
{
    public static class UserClaims
    {
        public static Claim[] Create(GetUsersDTO user) =>
        [
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        ];

        public static ClaimsPrincipal CreateCookiePrincipal(GetUsersDTO user) =>
            new(new ClaimsIdentity(Create(user), CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
