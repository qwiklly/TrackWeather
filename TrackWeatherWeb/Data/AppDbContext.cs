using Microsoft.EntityFrameworkCore;

namespace TrackWeatherWeb.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<ApplicationUsers> users { get; set; }
        public DbSet<TransportRequests> requests { get; set; }

    }
}
