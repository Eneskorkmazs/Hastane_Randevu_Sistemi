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
        public DbSet<MedicalReport> MedicalReports { get; set; }
        public DbSet<MedicalHistory> MedicalHistories { get; set; }
        public DbSet<HospitalReview> HospitalReviews { get; set; }
        public DbSet<DoctorReview> DoctorReviews { get; set; }

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

            modelBuilder.Entity<Appointment>()
                .Property(a => a.CollectedDate)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.AdminAccessRequestedDate)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.AdminAccessGrantedDate)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.ReminderSentAt)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.PrescriptionCreatedAt)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .Property(n => n.CreatedDate)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<MedicalReport>()
                .Property(m => m.UploadedAt)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<MedicalReport>()
                .HasOne(m => m.Appointment)
                .WithMany(a => a.MedicalReports)
                .HasForeignKey(m => m.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MedicalHistory>()
                .Property(m => m.VisitDate)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<MedicalHistory>()
                .Property(m => m.CreatedAt)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<MedicalHistory>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HospitalReview>()
                .Property(r => r.CreatedAt)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<HospitalReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HospitalReview>()
                .HasIndex(r => r.UserId)
                .HasDatabaseName("UX_HospitalReviews_UserId_NotNull")
                .IsUnique()
                .HasFilter(@"""UserId"" IS NOT NULL");

            modelBuilder.Entity<DoctorReview>()
                .Property(r => r.CreatedAt)
                .HasColumnType(dateTimeColumnType);

            modelBuilder.Entity<DoctorReview>()
                .HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Her kullanıcı her doktora yalnızca bir yorum bırakabilir
            modelBuilder.Entity<DoctorReview>()
                .HasIndex(r => new { r.DoctorId, r.UserId })
                .HasDatabaseName("UX_DoctorReviews_DoctorId_UserId")
                .IsUnique()
                .HasFilter(@"""UserId"" IS NOT NULL");
        }
    }
}
