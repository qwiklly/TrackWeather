using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static TrackWeatherWeb.Responses.CustomResponses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TrackWeatherWeb.Data;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace TrackWeatherWeb.Repositories
{
    public class Application(AppDbContext appDbContext, IConfiguration config) : IApplication
    {
        // Handles user login by verifying credentials and generating a JWT token if successful.
        public async Task<LoginResponse> LoginAsync(LoginDTO model)
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

        // Checks if the user already exists; if not, registers a new user by adding it to the database.
        public async Task<RegisterResponse> RegisterAsync(RegisterDTO model)
        {
            var findUser = await GetUserAsync(model.Email);
            if (findUser != null) return new RegisterResponse(false, "User already exist");

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

        // Retrieves a list of all users in the system
        public async Task<List<GetUsersDTO>> GetUsersAsync()
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

        // Retrieves a single user based on their email address.
        public async Task<ApplicationUsers?> GetUserAsync(string email)
            => await appDbContext.Users.FirstOrDefaultAsync(e => e.Email == email);

        // Updates an existing user's details if found, otherwise returns an error message.
        public async Task<ActionResult<RegisterResponse>> UpdateUserAsync(string email, RegisterDTO model)
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

        // Deletes a user by email if they exist, otherwise returns an error message.
        public async Task<BaseResponse> DeleteUserAsync(DeleteUserDTO model)
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

        // Retrieves all user coordinates.
        public async Task<List<RequestTransportDTO>> GetCoordinatesAsync()
        {
            return await appDbContext.Requests
                .Select(u => new RequestTransportDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    Coordinate_x = u.Coordinate_x,
                    Coordinate_y = u.Coordinate_y,
                    Comment = u.Comment,
                    Timestamp = u.Timestamp
                })
                .ToListAsync();
        }


        // Retrieves a specific coordinate by id
        public async Task<TransportRequests?> GetCoordinateAsync(int Id)
            => await appDbContext.Requests.FirstOrDefaultAsync(e => e.Id == Id);

        // Deletes transportRequest.
        public async Task<BaseResponse> DeleteCoordinatesAsync(DeleteTransportRequestDTO model)
        {
            var coordinates = await GetCoordinateAsync(model.Id);
            if (coordinates != null)
            {
                appDbContext.Requests.Remove(coordinates);
                await appDbContext.SaveChangesAsync();
                return new BaseResponse(true, "Coordinates deleted successfully");
            }
            else
            {
                return new BaseResponse(false, "Coordinates not found");
            }
        }

        // Updates a specific coordinate entry by its ID.
        public async Task<ActionResult<BaseResponse>> UpdateCoordinatesAsync(int id, RequestTransportDTO model)
        {
            var coordinates = await GetCoordinateAsync(id);

            if (coordinates == null)
            {
                return new BaseResponse(false, $"Coordinates with ID '{id}' not found.");
            }

            coordinates.Coordinate_x = model.Coordinate_x;
            coordinates.Coordinate_y = model.Coordinate_y;
            coordinates.Comment = model.Comment ?? coordinates.Comment;

            coordinates.Timestamp = DateTime.UtcNow;

            await appDbContext.SaveChangesAsync();

            return new BaseResponse(true, "Coordinates updated successfully.");
        }

        //Adding coordinates
        public async Task<BaseResponse> RequestTransportAsync(RequestTransportDTO model)
        {
            appDbContext.Requests.Add(
                 new TransportRequests()
                 {
                     Email = model.Email,
                     Coordinate_x = model.Coordinate_x,
                     Coordinate_y = model.Coordinate_y,
                     Comment = model.Comment,
                     Timestamp = DateTime.UtcNow
                 });

            await appDbContext.SaveChangesAsync();
            return new BaseResponse(true, "Success");
        }
        public static async Task Create_Admin(AppDbContext context)
        {
            // Check if an admin already exists.
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                // Create a new admin user.
                var admin = new ApplicationUsers
                {
                    Role = "Admin",
                    Name = "Admin",
                    Email = "admin@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("Admin123")
                };

                context.Users.Add(admin);
                await context.SaveChangesAsync();
            }
        }

        // Generates a JWT token based on the user's details.
        private string GenerateToken(ApplicationUsers user)
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
    }
}