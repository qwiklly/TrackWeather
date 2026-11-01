using Microsoft.EntityFrameworkCore;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.Data
{
    public class DatabaseInitializer(
        AppDbContext appDbContext,
        IConfiguration config,
        IHostEnvironment environment,
        ILogger<DatabaseInitializer> logger)
    {
        // The first migration seeds this account, and its password was published on the home page
        private const string LegacyAdminEmail = "admin@example.com";
        private const string LegacyAdminPassword = "Admin123";

        public async Task InitializeAsync()
        {
            if (config.GetValue<bool>("Database:MigrateOnStartup"))
            {
                await appDbContext.Database.MigrateAsync();
            }

            await SeedAdminAsync();

            if (!environment.IsDevelopment())
            {
                await LockLegacyAdminAsync();
            }
        }

        private async Task SeedAdminAsync()
        {
            var email = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
            var password = config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var admin = await appDbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (admin == null)
            {
                appDbContext.Users.Add(new ApplicationUsers
                {
                    Name = "Admin",
                    Email = email,
                    Role = Roles.Admin,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                });
            }
            else if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }

            await appDbContext.SaveChangesAsync();
        }

        private async Task LockLegacyAdminAsync()
        {
            var admin = await appDbContext.Users.FirstOrDefaultAsync(u => u.Email == LegacyAdminEmail);
            if (admin == null || !BCrypt.Net.BCrypt.Verify(LegacyAdminPassword, admin.PasswordHash))
            {
                return;
            }

            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
            await appDbContext.SaveChangesAsync();
            logger.LogWarning("Account {Email} still used the published password and has been locked. Set Seed:AdminEmail and Seed:AdminPassword to create an administrator", LegacyAdminEmail);
        }
    }
}
