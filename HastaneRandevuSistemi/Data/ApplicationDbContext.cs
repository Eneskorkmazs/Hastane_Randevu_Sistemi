using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany(dp => dp.Doctors)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.PatientUser)
                .WithMany()
                .HasForeignKey(a => a.PatientUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AppUser>()
                .Property(u => u.DogumTarihi)
                .HasColumnType("date");

            var provider = Database.ProviderName ?? string.Empty;
            var dateTimeColumnType = provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                ? "timestamp without time zone"
                : provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
                    ? "TEXT"
                    : "datetime2";

            modelBuilder.Entity<Appointment>()
                .Property(a => a.AppointmentDate)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.CreatedDate)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .Property(n => n.CreatedDate)
                .HasColumnType(dateTimeColumnType);
        }
    }
}
