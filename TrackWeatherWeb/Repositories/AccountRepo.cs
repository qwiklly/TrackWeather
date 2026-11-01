using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Options;
using TrackWeatherWeb.Repositories.Interfaces;
using static TrackWeatherWeb.Responses.CustomResponses;

namespace TrackWeatherWeb.Repositories
{
    public class AccountRepo(
        AppDbContext appDbContext,
        IOptions<JwtOptions> jwtOptions,
        IStringLocalizer<SharedResource> localizer) : IAccountRepo
    {
        // Verified when the email is unknown, so response time does not reveal which emails are registered
        private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

        // Handles user login by verifying credentials and generating a JWT token if successful.
        public async Task<LoginResponse> LoginAsync(LoginDTO model)
        {
            var user = await AuthenticateAsync(model);
            if (user == null)
            {
                return new LoginResponse(false, localizer["Account.InvalidCredentials"], Error: ResponseError.Unauthorized);
            }

            return new LoginResponse(true, localizer["Account.LoginSucceeded"], GenerateToken(user));
        }

        // Returns the user when the email and password match, otherwise null.
        public async Task<GetUsersDTO?> AuthenticateAsync(LoginDTO model)
        {
            var user = await FindUserAsync(model.Email);
            var passwordMatches = BCrypt.Net.BCrypt.Verify(model.Password, user?.PasswordHash ?? DummyHash);
            return user != null && passwordMatches ? ToDto(user) : null;
        }

        // Public registration: the role is never taken from the request.
        public Task<RegisterResponse> RegisterAsync(RegisterDTO model) => AddUserAsync(model, Roles.User);

        // Administrator action: the role is chosen explicitly.
        public Task<RegisterResponse> CreateUserAsync(CreateUserDTO model) => AddUserAsync(model, model.Role);

        private async Task<RegisterResponse> AddUserAsync(RegisterDTO model, string role)
        {
            if (await FindUserAsync(model.Email) != null)
            {
                return new RegisterResponse(false, localizer["Account.AlreadyExists"], ResponseError.Conflict);
            }

            // Add a new user to the database with hashed password.
            appDbContext.Users.Add(new ApplicationUsers
            {
                Role = role,
                Name = model.Name.Trim(),
                Email = NormalizeEmail(model.Email),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            });

            try
            {
                await appDbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two requests raced past the check above; the unique index caught the second one
                return new RegisterResponse(false, localizer["Account.AlreadyExists"], ResponseError.Conflict);
            }

            return new RegisterResponse(true, localizer["Account.Registered"]);
        }

        // Retrieves a list of all users in the system
        public Task<List<GetUsersDTO>> GetUsersAsync() =>
            appDbContext.Users
                .AsNoTracking()
                .OrderBy(u => u.Name)
                .Select(u => new GetUsersDTO { Role = u.Role, Name = u.Name, Email = u.Email })
                .ToListAsync();

        // Retrieves a single user based on their email address.
        public async Task<GetUsersDTO?> GetUserAsync(string email)
        {
            var user = await FindUserAsync(email);
            return user == null ? null : ToDto(user);
        }

        // Updates an existing user's details if found, otherwise returns an error message.
        public async Task<RegisterResponse> UpdateUserAsync(string email, UpdateUserDTO model)
        {
            var user = await FindUserAsync(email);
            if (user == null)
            {
                return new RegisterResponse(false, localizer["Account.NotFound"], ResponseError.NotFound);
            }

            if (user.Role == Roles.Admin && model.Role == Roles.User && await IsLastAdminAsync())
            {
                return new RegisterResponse(false, localizer["Account.LastAdmin"], ResponseError.Conflict);
            }

            // Update user's name and role only if new values are provided.
            user.Name = model.Name?.Trim() ?? user.Name;
            user.Role = model.Role ?? user.Role;

            // If a new password is provided, hash and update it.
            if (!string.IsNullOrEmpty(model.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            await appDbContext.SaveChangesAsync();
            return new RegisterResponse(true, localizer["Account.Updated"]);
        }

        // Deletes a user by email if they exist, otherwise returns an error message.
        public async Task<BaseResponse> DeleteUserAsync(string email, string? actingUserEmail)
        {
            var user = await FindUserAsync(email);
            if (user == null)
            {
                return new BaseResponse(false, localizer["Account.NotFound"], ResponseError.NotFound);
            }

            if (actingUserEmail != null && user.Email == NormalizeEmail(actingUserEmail))
            {
                return new BaseResponse(false, localizer["Account.CannotDeleteSelf"], ResponseError.Conflict);
            }

            if (user.Role == Roles.Admin && await IsLastAdminAsync())
            {
                return new BaseResponse(false, localizer["Account.LastAdmin"], ResponseError.Conflict);
            }

            appDbContext.Users.Remove(user);
            await appDbContext.SaveChangesAsync();
            return new BaseResponse(true, localizer["Account.Deleted"]);
        }

        private Task<ApplicationUsers?> FindUserAsync(string email)
        {
            var normalized = NormalizeEmail(email);
            return appDbContext.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        }

        private async Task<bool> IsLastAdminAsync() =>
            await appDbContext.Users.CountAsync(u => u.Role == Roles.Admin) <= 1;

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static GetUsersDTO ToDto(ApplicationUsers user) =>
            new() { Role = user.Role, Name = user.Name, Email = user.Email };

        // Generates a JWT token based on the user's details.
        private string GenerateToken(GetUsersDTO user)
        {
            var jwt = jwtOptions.Value;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwt.Issuer,
                audience: jwt.Audience,
                claims: UserClaims.Create(user),
                expires: DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
