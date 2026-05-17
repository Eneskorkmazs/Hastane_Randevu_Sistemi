using System.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Data
{
    public static class DbSeeder
    {
        public static async Task Seed(IServiceProvider service)
        {
            var context = service.GetRequiredService<ApplicationDbContext>();
            var userManager = service.GetRequiredService<UserManager<AppUser>>();
            var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = service.GetRequiredService<IConfiguration>();
            var environment = service.GetRequiredService<IHostEnvironment>();
            var logger = service.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            // Veritabanı şemasını her zaman güncelle
            await context.Database.MigrateAsync();

            // Kesin Çözüm: Eczane tablosunu sıfırla ki yeni sütunlar (Lat/Long) kesin oluşsun
            try {
                await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS Pharmacies;");
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE Pharmacies (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT, City TEXT, District TEXT, Address TEXT, 
                        Phone TEXT, Email TEXT, Website TEXT, 
                        IsOnDuty INTEGER, DutyDate TEXT, 
                        OpenTime TEXT, CloseTime TEXT,
                        Latitude REAL, Longitude REAL, UserId TEXT
                    );");
            } catch { }

            await EnsureRolesAsync(roleManager);
            await EnsureAccountingColumnsAsync(context);
            await EnsureReminderColumnsAsync(context);
            await EnsurePrescriptionColumnsAsync(context);
            await EnsureMedicalReportsTableAsync(context);
            await EnsureMedicalHistoryTableAsync(context);
            await EnsureHospitalReviewsTableAsync(context);
            await EnsureDoctorReviewsTableAsync(context);
            await EnsureAdminAsync(userManager, configuration, environment, logger);
            await EnsureDemoPatientAsync(userManager, configuration, environment, logger);
            await EnsureDemoSecretaryAsync(userManager, configuration, environment, logger);
            await NormalizeDepartmentsAsync(context);
            await SeedDepartmentsAndDoctorsAsync(context, userManager, configuration, environment, logger);
            await SeedDemoAppointmentsAsync(context, environment, logger);
            await SeedTodayQueueAppointmentsAsync(context, environment, logger);
            await EnsurePharmaciesAsync(context);

            await context.SaveChangesAsync();
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Doktor", "Hasta", "Sekreter" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static Task EnsureAccountingColumnsAsync(ApplicationDbContext context) => Task.CompletedTask;
        private static Task EnsureReminderColumnsAsync(ApplicationDbContext context) => Task.CompletedTask;
        private static Task EnsurePrescriptionColumnsAsync(ApplicationDbContext context) => Task.CompletedTask;
        private static Task EnsureMedicalReportsTableAsync(ApplicationDbContext context) => Task.CompletedTask;
        private static Task EnsureMedicalHistoryTableAsync(ApplicationDbContext context) => Task.CompletedTask;
        private static Task EnsureHospitalReviewsTableAsync(ApplicationDbContext context) => Task.CompletedTask;
        private static Task EnsureDoctorReviewsTableAsync(ApplicationDbContext context) => Task.CompletedTask;

        private static async Task EnsureAdminAsync(UserManager<AppUser> userManager, IConfiguration configuration, IHostEnvironment environment, ILogger logger)
        {
            var adminEmail = "admin@hastane.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new AppUser { UserName = adminEmail, Email = adminEmail, Name = "Admin", Surname = "User", EmailConfirmed = true };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        private static async Task EnsureDemoPatientAsync(UserManager<AppUser> userManager, IConfiguration configuration, IHostEnvironment environment, ILogger logger)
        {
            var patientEmail = "hasta@hastane.com";
            if (await userManager.FindByEmailAsync(patientEmail) == null)
            {
                var patient = new AppUser { UserName = patientEmail, Email = patientEmail, Name = "Enes", Surname = "Korkmaz", TC = "12345678901", Telefon = "05555555555", DogumTarihi = new DateTime(1995, 1, 1), Cinsiyet = "Erkek", Adres = "İstanbul", EmailConfirmed = true };
                await userManager.CreateAsync(patient, "Hasta123!");
                await userManager.AddToRoleAsync(patient, "Hasta");
            }
        }

        private static async Task EnsureDemoSecretaryAsync(UserManager<AppUser> userManager, IConfiguration configuration, IHostEnvironment environment, ILogger logger)
        {
            var secretaryEmail = "sekreter@hastane.com";
            if (await userManager.FindByEmailAsync(secretaryEmail) == null)
            {
                var secretary = new AppUser { UserName = secretaryEmail, Email = secretaryEmail, Name = "Sekreter", Surname = "User", EmailConfirmed = true };
                await userManager.CreateAsync(secretary, "Sekreter123!");
                await userManager.AddToRoleAsync(secretary, "Sekreter");
            }
        }

        private static Task NormalizeDepartmentsAsync(ApplicationDbContext context) => Task.CompletedTask;

        private static async Task SeedDepartmentsAndDoctorsAsync(ApplicationDbContext context, UserManager<AppUser> userManager, IConfiguration configuration, IHostEnvironment environment, ILogger logger)
        {
            if (!context.Departments.Any())
            {
                var depts = new List<Department>
                {
                    new Department { Name = "Dahiliye", Description = "İç Hastalıkları" },
                    new Department { Name = "Göz", Description = "Göz Hastalıkları" },
                    new Department { Name = "Kardiyoloji", Description = "Kalp Hastalıkları" }
                };
                context.Departments.AddRange(depts);
                await context.SaveChangesAsync();

                foreach (var dept in depts)
                {
                    var doctorEmail = $"doktor_{dept.Name.ToLower()}@hastane.com";
                    if (await userManager.FindByEmailAsync(doctorEmail) == null)
                    {
                        var doctorUser = new AppUser { UserName = doctorEmail, Email = doctorEmail, Name = "Doktor", Surname = dept.Name, EmailConfirmed = true };
                        await userManager.CreateAsync(doctorUser, "Doktor123!");
                        await userManager.AddToRoleAsync(doctorUser, "Doktor");

                        var doctor = new Doctor { UserId = doctorUser.Id, Name = "Doktor", Surname = dept.Name, DepartmentId = dept.Id, Title = "Uzm. Dr." };
                        context.Doctors.Add(doctor);
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedDemoAppointmentsAsync(ApplicationDbContext context, IHostEnvironment environment, ILogger logger)
        {
            var doctors = await context.Doctors
                .Include(d => d.Department)
                .OrderBy(d => d.Id)
                .ToListAsync();

            if (!doctors.Any())
            {
                return;
            }

            const string demoPhonePrefix = "05000002";
            var existingDemoCount = await context.Appointments
                .CountAsync(a => a.PatientPhone != null && a.PatientPhone.StartsWith(demoPhonePrefix));

            if (existingDemoCount >= 12)
            {
                return;
            }

            var today = DateTime.Today;
            var demoPatients = new[]
            {
                ("Ali", "Yilmaz"),
                ("Ayse", "Demir"),
                ("Mehmet", "Kaya"),
                ("Fatma", "Celik"),
                ("Murat", "Sahin"),
                ("Elif", "Aydin"),
                ("Can", "Arslan"),
                ("Zeynep", "Koc"),
                ("Emre", "Ozkan"),
                ("Derya", "Yildiz"),
                ("Burak", "Aslan"),
                ("Selin", "Eren")
            };

            var appointments = new List<Appointment>();
            for (var i = existingDemoCount; i < 12; i++)
            {
                var doctor = doctors[i % doctors.Count];
                var patient = demoPatients[i];
                var appointmentDate = today
                    .AddDays(-12 + i)
                    .AddHours(9 + (i % 7))
                    .AddMinutes((i % 2) * 30);

                appointments.Add(new Appointment
                {
                    AppointmentDate = appointmentDate,
                    PatientName = patient.Item1,
                    PatientSurname = patient.Item2,
                    PatientPhone = $"{demoPhonePrefix}{i:D2}",
                    DoctorId = doctor.Id,
                    Status = AppointmentStatus.Tamamlandi,
                    CreatedDate = appointmentDate.AddDays(-3),
                    ApprovedDate = appointmentDate.AddDays(-2),
                    ApprovedByName = "Demo Sekreter",
                    IsCollected = i % 3 != 0,
                    CollectedDate = i % 3 != 0 ? appointmentDate.AddMinutes(20) : null,
                    Price = 850 + (i % 4) * 125,
                    PrescriptionDiagnosis = null,
                    PrescriptionMedications = null,
                    PrescriptionNotes = null,
                    PrescriptionCreatedAt = null,
                    PrescriptionSentAt = null,
                    PrescriptionSentByName = null,
                    PharmacyId = null,
                    PharmacyStatus = PrescriptionPharmacyStatus.Yok
                });
            }

            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();
        }
        private static Task SeedTodayQueueAppointmentsAsync(ApplicationDbContext context, IHostEnvironment environment, ILogger logger) => Task.CompletedTask;

        private static async Task EnsurePharmaciesAsync(ApplicationDbContext context)
        {
            // Tabloyu zaten Seed başında SQL ile sıfırlıyoruz.
            var today = DateTime.Today;
            var pharmacies = new List<Pharmacy>
            {
                // SİNOP - AYANCIK
                new Pharmacy { Name = "Merkez Eczanesi", City = "Sinop", District = "Ayancık", Address = "Yalı Mah. Cumhuriyet Cad. No:1", Phone = "0368 715 00 00", IsOnDuty = true, DutyDate = today, OpenTime = "00:00", CloseTime = "23:59", Latitude = 41.9447, Longitude = 34.5867 },
                new Pharmacy { Name = "Şifa Eczanesi", City = "Sinop", District = "Ayancık", Address = "Cevizli Mah. Hastane Yolu No:12", Phone = "0368 715 11 11", IsOnDuty = false, OpenTime = "08:30", CloseTime = "19:00", Latitude = 41.9432, Longitude = 34.5881 },
                new Pharmacy { Name = "Sağlık Eczanesi", City = "Sinop", District = "Ayancık", Address = "Orta Mah. Pazar Cad. No:5", Phone = "0368 715 22 22", IsOnDuty = false, OpenTime = "08:00", CloseTime = "20:00", Latitude = 41.9460, Longitude = 34.5850 },
                new Pharmacy { Name = "Ayancık Eczanesi", City = "Sinop", District = "Ayancık", Address = "Yalı Mah. Atatürk Cad. No:8", Phone = "0368 715 33 33", IsOnDuty = false, OpenTime = "08:30", CloseTime = "19:30", Latitude = 41.9452, Longitude = 34.5875 },
                new Pharmacy { Name = "Güneş Eczanesi", City = "Sinop", District = "Ayancık", Address = "Yalı Mah. Dr. Azmi Hamzaoğlu Cad.", Phone = "0368 715 44 44", IsOnDuty = false, OpenTime = "09:00", CloseTime = "19:00", Latitude = 41.9440, Longitude = 34.5890 },
                // İSTANBUL
                new Pharmacy { Name = "Beşiktaş Eczanesi", City = "İstanbul", District = "Beşiktaş", Address = "Sinanpaşa Mah. Ortabahçe Cad.", Phone = "0212 236 10 10", IsOnDuty = true, DutyDate = today, OpenTime = "00:00", CloseTime = "23:59", Latitude = 41.0428, Longitude = 29.0075 },
                new Pharmacy { Name = "Moda Eczanesi", City = "İstanbul", District = "Kadıköy", Address = "Caferağa Mah. Moda Cad.", Phone = "0216 336 20 20", IsOnDuty = false, OpenTime = "08:30", CloseTime = "19:00", Latitude = 40.9850, Longitude = 29.0250 },
                new Pharmacy { Name = "İstiklal Eczanesi", City = "İstanbul", District = "Beyoğlu", Address = "İstiklal Cad. No:150", Phone = "0212 244 30 30", IsOnDuty = false, OpenTime = "08:00", CloseTime = "20:00", Latitude = 41.0340, Longitude = 28.9780 },
                // ANKARA
                new Pharmacy { Name = "Kızılay Eczanesi", City = "Ankara", District = "Çankaya", Address = "Kızılay Meydanı No:5", Phone = "0312 417 55 55", IsOnDuty = true, DutyDate = today, OpenTime = "00:00", CloseTime = "23:59", Latitude = 39.9208, Longitude = 32.8541 },
                new Pharmacy { Name = "Ulus Eczanesi", City = "Ankara", District = "Altındağ", Address = "Anafartalar Mah. Ulus Meydanı", Phone = "0312 311 66 66", IsOnDuty = false, OpenTime = "09:00", CloseTime = "19:00", Latitude = 39.9411, Longitude = 32.8509 },
                // BURSA
                new Pharmacy { Name = "Ulu Cami Eczanesi", City = "Bursa", District = "Osmangazi", Address = "Nalbantoğlu Mah. Atatürk Cad.", Phone = "0224 221 50 50", IsOnDuty = true, DutyDate = today, OpenTime = "00:00", CloseTime = "23:59", Latitude = 40.1826, Longitude = 29.0665 },
                // TRABZON
                new Pharmacy { Name = "Boztepe Eczanesi", City = "Trabzon", District = "Ortahisar", Address = "Boztepe Mah. İran Cad.", Phone = "0462 321 70 70", IsOnDuty = true, DutyDate = today, OpenTime = "00:00", CloseTime = "23:59", Latitude = 40.9959, Longitude = 39.7285 },
                // ŞANLIURFA (Mevcut 4 Eczane)
                new Pharmacy { Name = "Balıklıgöl Eczanesi", City = "Şanlıurfa", District = "Eyyübiye", Address = "Gölbaşı Mah. Balıklıgöl Cad.", Phone = "0414 215 10 10", IsOnDuty = true, DutyDate = today, OpenTime = "00:00", CloseTime = "23:59", Latitude = 37.1591, Longitude = 38.7969 },
                new Pharmacy { Name = "Göbeklitepe Eczanesi", City = "Şanlıurfa", District = "Haliliye", Address = "Bahçelievler Mah. Atatürk Bulv.", Phone = "0414 313 20 20", IsOnDuty = false, OpenTime = "08:30", CloseTime = "19:30", Latitude = 37.1674, Longitude = 38.8012 },
                new Pharmacy { Name = "Harran Eczanesi", City = "Şanlıurfa", District = "Haliliye", Address = "Yenice Mah. Harran Üniversitesi Cad.", Phone = "0414 318 24 24", IsOnDuty = false, OpenTime = "08:00", CloseTime = "20:00", Latitude = 37.1712, Longitude = 38.8145 },
                new Pharmacy { Name = "Acı Biber Eczanesi", City = "Şanlıurfa", District = "Haliliye", Address = "İsotçular Çarşısı Yanı, No:63", Phone = "0414 633 63 63", IsOnDuty = true, DutyDate = today, OpenTime = "00:00", CloseTime = "23:59", Latitude = 37.1620, Longitude = 38.7950 },
                // ŞANLIURFA (Eklenen 5 Yeni Eczane)
                new Pharmacy { Name = "Karaköprü Eczanesi", City = "Şanlıurfa", District = "Karaköprü", Address = "Atakent Mah. Diyarbakır Yolu Bulvarı No:45", Phone = "0414 345 50 50", IsOnDuty = false, OpenTime = "08:30", CloseTime = "19:00", Latitude = 37.2015, Longitude = 38.8055 },
                new Pharmacy { Name = "Siverek Şifa Eczanesi", City = "Şanlıurfa", District = "Siverek", Address = "Kale Mah. Cumhuriyet Cad. No:12", Phone = "0414 552 11 22", IsOnDuty = false, OpenTime = "08:00", CloseTime = "20:00", Latitude = 37.7550, Longitude = 39.3150 },
                new Pharmacy { Name = "Birecik Baraj Eczanesi", City = "Şanlıurfa", District = "Birecik", Address = "Meydan Mah. Sahil Yolu Cad. No:8", Phone = "0414 652 33 44", IsOnDuty = true, DutyDate = today, OpenTime = "00:00", CloseTime = "23:59", Latitude = 37.0250, Longitude = 37.9850 },
                new Pharmacy { Name = "Viranşehir Güneş Eczanesi", City = "Şanlıurfa", District = "Viranşehir", Address = "Yenişehir Mah. Ceylanpınar Cad. No:24", Phone = "0414 811 44 55", IsOnDuty = false, OpenTime = "08:30", CloseTime = "19:30", Latitude = 37.2350, Longitude = 39.7650 },
                new Pharmacy { Name = "Urfa Meydan Eczanesi", City = "Şanlıurfa", District = "Eyyübiye", Address = "Haşimiye Meydanı, Divanyolu Cad. No:3", Phone = "0414 215 77 88", IsOnDuty = false, OpenTime = "08:00", CloseTime = "19:30", Latitude = 37.1520, Longitude = 38.7910 }
            };

            await context.Pharmacies.AddRangeAsync(pharmacies);
            await context.SaveChangesAsync();
        }
    }
}
