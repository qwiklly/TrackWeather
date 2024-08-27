using Microsoft.EntityFrameworkCore;
using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<ApplicationUsers> Users { get; set; }
        public DbSet<TransportRequests> Requests { get; set; }
    }
}
