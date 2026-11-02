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

            var requests = modelBuilder.Entity<TransportRequests>();

            // Timestamps are stored in UTC; marking them so makes JSON carry the "Z" and browsers show local time
            requests.Property(r => r.Timestamp)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            requests.Property(r => r.UpdatedAt)
                .HasConversion(v => v, v => v == null ? null : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

            requests.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(TransportStatus.New)
                .HasSentinel((TransportStatus)(-1));

            requests.HasIndex(r => r.Status);
            requests.HasIndex(r => r.Email);
            requests.HasIndex(r => r.AssignedToEmail);
        }
    }
}
