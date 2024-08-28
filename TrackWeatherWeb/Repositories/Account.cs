using Microsoft.IdentityModel.Tokens;
using static TrackWeatherWeb.Responses.CustomResponses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace TrackWeatherWeb.Repositories
{
    public class Account(AppDbContext appDbContext, IConfiguration config) : IAccount
    {
        // Handles user login by verifying credentials and generating a JWT token if successful.
        public async Task<LoginResponse> LoginAsync(LoginDTO model)
        {
            try
            {
                var findUser = await GetUserAsync(model.Email);
                if (findUser == null) return new LoginResponse(false, "User not found");

                // Verifies the password using BCrypt.
                if (!BCrypt.Net.BCrypt.Verify(model.Password, findUser.Password))
                    return new LoginResponse(false, "Email or Pass incorrect");

                // Generates a JWT token for the authenticated user.
                string jwtToken = GenerateToken(findUser);
                return new LoginResponse(true, "Login successfully", jwtToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while signing in");
                //display user exception
                return new LoginResponse(false, "Error occurred while signing in");
            }
        }

        // Checks if the user already exists; if not, registers a new user by adding it to the database.
        public async Task<RegisterResponse> RegisterAsync(RegisterDTO model)
        {
            try
            {
                var findUser = await GetUserAsync(model.Email);
                if (findUser != null) return new RegisterResponse(false, "User already exist");

				if (model.Password != model.ConfirmPassword)
					return new RegisterResponse(false, "Passwords do not match");

				// Add a new user to the database with hashed password.
				appDbContext.Users.Add(
                     new ApplicationUsers()
                     {
                         Role = model.Role,
                         Name = model.Name,
                         Email = model.Email,
                         Password = BCrypt.Net.BCrypt.HashPassword(model.Password)
                     });

                await appDbContext.SaveChangesAsync();
                return new RegisterResponse(true, "User registrated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while signing up");
                //display user exception
                return new RegisterResponse(false, "Error occurred while signing up");
            }
        }

        // Retrieves a list of all users in the system
        public async Task<List<GetUsersDTO>> GetUsersAsync()
        {
            try
            {
                var users = await appDbContext.Users
                .Select(u => new GetUsersDTO
                {
                    Role = u.Role,
                    Name = u.Name,
                    Email = u.Email
                })
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while finding users");
                //display user exception
                throw new InvalidOperationException("Error while finding users ");
            }
        }
        // Retrieves a single user based on their email address.
        public async Task<ApplicationUsers?> GetUserAsync(string email)
            => await appDbContext.Users.FirstOrDefaultAsync(e => e.Email == email);

        // Updates an existing user's details if found, otherwise returns an error message.
        public async Task<RegisterResponse> UpdateUserAsync(string email, RegisterDTO model)
        {
            try
            {
                // Find the user by their email.
                var user = await GetUserAsync(email);

                if (user == null)
                {
                    return new RegisterResponse(false, $"User with email '{email}' not found.");
                }

                // Update user's name and role only if new values are provided.
                user.Name = model.Name ?? user.Name;
                user.Role = model.Role ?? user.Role;

                // If a new password is provided, hash and update it.
                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                await appDbContext.SaveChangesAsync();

                return new RegisterResponse(true, "User updated successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while updating user's data");
                //display user exception
                return new RegisterResponse(false, "Error while updating user's data ");
            }
        }

        // Deletes a user by email if they exist, otherwise returns an error message.
        public async Task<BaseResponse> DeleteUserAsync(DeleteUserDTO model)
        {
            try
            {
                var user = await GetUserAsync(model.Email);
                if (user != null)
                {
                    appDbContext.Users.Remove(user);
                    await appDbContext.SaveChangesAsync();
                    return new BaseResponse(true, "User deleted successfully");
                }
                else
                {
                    return new BaseResponse(false, "User not found");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while deleting user");
                //display user exception
                return new BaseResponse(false, "Error while deleting user ");
            }
        }
        // Generates a JWT token based on the user's details.
        private string GenerateToken(ApplicationUsers user)
        {
            try
            {
                // Generate security key and credentials.
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                // Define the user claims to be included in the token.
                var userClaims = new[]
                {
                new Claim(ClaimTypes.Name, user.Name!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!),
            };
                var token = new JwtSecurityToken(
                    issuer: config["Jwt:Issuer"]!,
                    audience: config["Jwt:Audience"]!,
                    claims: userClaims,
                    expires: DateTime.Now.AddDays(2),
                    signingCredentials: credentials
                    );
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error while Generating Token");

                throw new Exception("Error while Generating Token ");
            }
        }
    }
}