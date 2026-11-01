using Microsoft.EntityFrameworkCore;
using TrackWeatherWeb.Models;

namespace TrackWeatherWeb.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
    {
        public DbSet<ApplicationUsers> Users { get; set; }
        public DbSet<TransportRequests> Requests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUsers>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Timestamps are stored in UTC; marking them so makes JSON carry the "Z" and browsers show local time
            modelBuilder.Entity<TransportRequests>()
                .Property(r => r.Timestamp)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        }
    }
}
