using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.States
{
    public class AuthenticationProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClaimsPrincipal anonymous = new(new ClaimsIdentity());

        public AuthenticationProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var jwtToken = GetJwtTokenFromCookie();
                if (string.IsNullOrEmpty(jwtToken))
                    return await Task.FromResult(new AuthenticationState(anonymous));

                var getUserClaims = DecryptToken(jwtToken);
                if (getUserClaims == null)
                    return await Task.FromResult(new AuthenticationState(anonymous));

                var claimsPrincipal = SetClaimPrincipal(getUserClaims);
                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch
            {
                return await Task.FromResult(new AuthenticationState(anonymous));
            }
        }

        public async Task UpdateAuthenticationState(string? jwtToken)
        {
                var claimsPrincipal = new ClaimsPrincipal();
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    Constants.JWTToken = jwtToken;
                    var getUserClaims = DecryptToken(jwtToken);
                    claimsPrincipal = SetClaimPrincipal(getUserClaims);
                await SetJwtTokenInCookie(jwtToken);
                }
                else
                {
                    Constants.JWTToken = null!;
                RemoveJwtTokenFromCookie();
                }

                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        private async Task SetJwtTokenInCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {

                HttpOnly = true, 
                Secure = false,  
                Expires = DateTime.UtcNow.AddDays(4),
                SameSite = SameSiteMode.Lax // http -> https
            };


            await Task.Run(() =>
            {
                _httpContextAccessor.HttpContext?.Response.Cookies.Append("AuthToken", token, cookieOptions);
            });
        }


        private string? GetJwtTokenFromCookie()
        {
            if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("AuthToken", out string? token) == true)
            {
                return token;
            }

            return null;
        }


        private void RemoveJwtTokenFromCookie()
        {
            if (_httpContextAccessor.HttpContext?.Request.Cookies.ContainsKey("AuthToken") == true)
            {
                _httpContextAccessor.HttpContext.Response.Cookies.Delete("AuthToken");
            }
        }

        private static ClaimsPrincipal SetClaimPrincipal(CustomUserClaims claims)
        {
            if (claims.Email is null) return new ClaimsPrincipal();
            return new ClaimsPrincipal(new ClaimsIdentity(
                new List<Claim>
                {
                    new(ClaimTypes.Name, claims.Name!),
                    new(ClaimTypes.Email, claims.Email!),
                    new(ClaimTypes.Role, claims.Role!),
                }, "JwtAuth"));
        }

        private static CustomUserClaims DecryptToken(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken)) return new CustomUserClaims();
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);
            var name = token.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Name);
            var email = token.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Email);
            var role = token.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Role);
            return new CustomUserClaims(name!.Value, email!.Value, role!.Value);
        }
    }
}
