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

            var provider = context.Database.ProviderName ?? string.Empty;
            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            await EnsureRolesAsync(roleManager);
            await EnsureAccountingColumnsAsync(context);
            await EnsureAdminAsync(userManager, configuration, environment, logger);
            await NormalizeDepartmentsAsync(context);
            await SeedDepartmentsAndDoctorsAsync(context, userManager, configuration, environment, logger);

            await context.SaveChangesAsync();
        }

        private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Doktor", "Hasta" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task EnsureAccountingColumnsAsync(ApplicationDbContext context)
        {
            var provider = context.Database.ProviderName ?? string.Empty;

            if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                if (!await ColumnExistsAsync(context, "Appointments", "IsCollected"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""IsCollected"" INTEGER NOT NULL DEFAULT 0;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "CollectedDate"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""CollectedDate"" TEXT NULL;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "AdminAccessRequested"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""AdminAccessRequested"" INTEGER NOT NULL DEFAULT 0;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "AdminAccessRequestedDate"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""AdminAccessRequestedDate"" TEXT NULL;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "AdminAccessRequestedByUserId"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""AdminAccessRequestedByUserId"" TEXT NULL;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "AdminAccessRequestedByName"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""AdminAccessRequestedByName"" TEXT NULL;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "AdminAccessGranted"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""AdminAccessGranted"" INTEGER NOT NULL DEFAULT 0;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "AdminAccessGrantedDate"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""AdminAccessGrantedDate"" TEXT NULL;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "AdminAccessGrantedByUserId"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""AdminAccessGrantedByUserId"" TEXT NULL;");
                }

                if (!await ColumnExistsAsync(context, "Appointments", "AdminAccessGrantedByName"))
                {
                    await context.Database.ExecuteSqlRawAsync(
                        @"ALTER TABLE ""Appointments"" ADD COLUMN ""AdminAccessGrantedByName"" TEXT NULL;");
                }

                return;
            }

            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""IsCollected"" boolean NOT NULL DEFAULT FALSE;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CollectedDate"" timestamp without time zone NULL;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AdminAccessRequested"" boolean NOT NULL DEFAULT FALSE;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AdminAccessRequestedDate"" timestamp without time zone NULL;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AdminAccessRequestedByUserId"" character varying(450) NULL;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AdminAccessRequestedByName"" character varying(200) NULL;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AdminAccessGranted"" boolean NOT NULL DEFAULT FALSE;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AdminAccessGrantedDate"" timestamp without time zone NULL;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AdminAccessGrantedByUserId"" character varying(450) NULL;");
                await context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AdminAccessGrantedByName"" character varying(200) NULL;");
            }
        }

        private static async Task<bool> ColumnExistsAsync(
            ApplicationDbContext context,
            string tableName,
            string columnName)
        {
            await using var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = $@"PRAGMA table_info(""{tableName}"");";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task EnsureAdminAsync(
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            IHostEnvironment environment,
            ILogger logger)
        {
            const string adminEmail = "admin@havatakip.com.tr";
            if (await userManager.FindByEmailAsync(adminEmail) != null)
            {
                return;
            }

            var adminPassword = ResolveSeedPassword(
                configuration["SeedSettings:AdminPassword"],
                environment,
                "DevAdmin123!",
                logger,
                "Admin");

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Name = "Sistem",
                Surname = "Yoneticisi",
                EmailConfirmed = true
            };

            var createAdminResult = await userManager.CreateAsync(admin, adminPassword);
            if (createAdminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        private static async Task NormalizeDepartmentsAsync(ApplicationDbContext context)
        {
            var renameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Dahiliye (Ic Hastaliklari)", "Dahiliye (İç Hastalıkları)" },
                { "Dis Hastaliklari", "Diş Sağlığı ve Hastalıkları" },
                { "Dis Sagligi ve Hastaliklari", "Diş Sağlığı ve Hastalıkları" },
                { "Noroloji", "Nöroloji" },
                { "Goz Hastaliklari", "Göz Hastalıkları" },
                { "Kulak Burun Bogaz", "Kulak Burun Boğaz" },
                { "Kadin Hastaliklari ve Dogum", "Kadın Hastalıkları ve Doğum" },
                { "Gogus Hastaliklari", "Göğüs Hastalıkları" },
                { "Enfeksiyon Hastaliklari", "Enfeksiyon Hastalıkları" },
                { "Uroloji", "Üroloji" },
                { "Beyin ve Sinir Cerrahisi", "Beyin ve Sinir Cerrahisi" }
            };

            var existingDepartments = await context.Departments.ToListAsync();
            foreach (var dep in existingDepartments)
            {
                if (renameMap.TryGetValue(dep.Name, out var normalized))
                {
                    dep.Name = normalized;
                }
            }

            if (existingDepartments.Count > 0)
            {
                await context.SaveChangesAsync();
            }

            var mergedDepartments = await context.Departments
                .Include(d => d.Doctors)
                .ToListAsync();

            foreach (var group in mergedDepartments
                         .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                         .Where(g => g.Count() > 1))
            {
                var keeper = group.First();
                foreach (var duplicate in group.Skip(1))
                {
                    if (duplicate.Doctors != null)
                    {
                        foreach (var doctor in duplicate.Doctors)
                        {
                            doctor.DepartmentId = keeper.Id;
                        }
                    }

                    context.Departments.Remove(duplicate);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedDepartmentsAndDoctorsAsync(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IConfiguration configuration,
            IHostEnvironment environment,
            ILogger logger)
        {
            var doctorPassword = ResolveSeedPassword(
                configuration["SeedSettings:DoctorPassword"],
                environment,
                "DevDoctor123!",
                logger,
                "Doktor");

            if (string.IsNullOrWhiteSpace(doctorPassword))
            {
                return;
            }

            var hospitalData = new Dictionary<string, List<string>>
            {
                { "Diş Sağlığı ve Hastalıkları", new List<string> { "Uzm. Dr. Elif Yılmaz", "Uzm. Dr. Mehmet Korkmaz" } },
                { "Dahiliye (İç Hastalıkları)", new List<string> { "Prof. Dr. Canan Karatay", "Uzm. Dr. Ahmet Maranki" } },
                { "Kardiyoloji", new List<string> { "Prof. Dr. Mehmet Öz", "Doç. Dr. Bingür Sönmez" } },
                { "Nöroloji", new List<string> { "Prof. Dr. Gazi Yaşargil", "Uzm. Dr. Serdar Dağ" } },
                { "Ortopedi ve Travmatoloji", new List<string> { "Op. Dr. Feridun Kunak", "Prof. Dr. Burhan Uslu" } },
                { "Göz Hastalıkları", new List<string> { "Op. Dr. Kudret Göz", "Uzm. Dr. Levent Akçay" } },
                { "Kulak Burun Boğaz", new List<string> { "Op. Dr. Aytuğ Altundağ", "Prof. Dr. İbrahim Saraçoğlu", "Uzm. Dr. Deniz Erdem" } },
                { "Genel Cerrahi", new List<string> { "Prof. Dr. Münci Kalayoğlu", "Op. Dr. Ender Saraç" } },
                { "Dermatoloji", new List<string> { "Uzm. Dr. Nihat Hatipoğlu", "Dr. Şeyma Subaşı" } },
                { "Pediatri", new List<string> { "Uzm. Dr. Osman Müftüoğlu", "Dr. Sami Ulus" } },
                { "Psikiyatri", new List<string> { "Prof. Dr. İlber Ortaylı", "Dr. Gülseren Budayıcıoğlu", "Prof. Dr. Ahmet Korkmaz" } },
                { "Üroloji", new List<string> { "Op. Dr. Haydar Dümen", "Prof. Dr. Kemal Özkan" } },
                { "Fizik Tedavi ve Rehabilitasyon", new List<string> { "Uzm. Dr. Halit Yerebakan", "Dr. Ferhat Göçer" } },
                { "Kadın Hastalıkları ve Doğum", new List<string> { "Op. Dr. Banu Çiftçi", "Prof. Dr. Teksen Çamlıbel" } },
                { "Göğüs Hastalıkları", new List<string> { "Prof. Dr. Ahmet Rasim Küçükusta", "Uzm. Dr. Tevfik Özlü" } },
                { "Enfeksiyon Hastalıkları", new List<string> { "Prof. Dr. Mehmet Ceyhan", "Doç. Dr. Ateş Kara" } },
                { "Beyin ve Sinir Cerrahisi", new List<string> { "Prof. Dr. Yakup Yazıcı", "Prof. Dr. Enes Korkmaz" } }
            };

            foreach (var item in hospitalData)
            {
                var department = await context.Departments.FirstOrDefaultAsync(d => d.Name == item.Key);
                if (department == null)
                {
                    department = new Department { Name = item.Key };
                    await context.Departments.AddAsync(department);
                    await context.SaveChangesAsync();
                }

                foreach (var doctorDisplayName in item.Value)
                {
                    var (title, name, surname) = ParseDoctorIdentity(doctorDisplayName);
                    var email = BuildDoctorEmail(name, surname);

                    var existingUser = await userManager.FindByEmailAsync(email);
                    if (existingUser == null)
                    {
                        var user = new AppUser
                        {
                            UserName = email,
                            Email = email,
                            Name = name,
                            Surname = surname,
                            EmailConfirmed = true
                        };

                        var createResult = await userManager.CreateAsync(user, doctorPassword);
                        if (!createResult.Succeeded)
                        {
                            continue;
                        }

                        await userManager.AddToRoleAsync(user, "Doktor");
                        existingUser = user;
                    }
                    else if (!await userManager.IsInRoleAsync(existingUser, "Doktor"))
                    {
                        await userManager.AddToRoleAsync(existingUser, "Doktor");
                    }

                    var doctorExists = await context.Doctors.AnyAsync(d => d.UserId == existingUser.Id ||
                        (d.Name == name && d.Surname == surname && d.DepartmentId == department.Id));

                    if (doctorExists)
                    {
                        continue;
                    }

                    await context.Doctors.AddAsync(new Doctor
                    {
                        Name = name,
                        Surname = surname,
                        Title = title,
                        DepartmentId = department.Id,
                        UserId = existingUser.Id
                    });
                }
            }
        }

        private static string BuildDoctorEmail(string name, string surname)
        {
            var normalizedName = ConvertToIdentifier(name).Replace(" ", ".").Trim('.');
            var normalizedSurname = ConvertToIdentifier(surname).Replace(" ", ".").Trim('.');

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                normalizedName = "doktor";
            }

            if (string.IsNullOrWhiteSpace(normalizedSurname))
            {
                normalizedSurname = "hrs";
            }

            return $"{normalizedName}.{normalizedSurname}@havatakip.com.tr".ToLowerInvariant();
        }

        private static (string Title, string Name, string Surname) ParseDoctorIdentity(string displayName)
        {
            var tokens = displayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var titleTokens = new List<string>();
            while (tokens.Count > 1 && IsTitleToken(tokens[0]))
            {
                titleTokens.Add(tokens[0]);
                tokens.RemoveAt(0);
            }

            if (tokens.Count == 0)
            {
                return ("Uzm. Dr.", string.Empty, string.Empty);
            }

            var surname = tokens[^1];
            var name = string.Join(" ", tokens.Take(tokens.Count - 1));
            if (string.IsNullOrWhiteSpace(name))
            {
                name = surname;
                surname = string.Empty;
            }

            var rawTitle = titleTokens.Count > 0 ? string.Join(" ", titleTokens) : "Uzm. Dr.";
            return (NormalizeTitle(rawTitle), name, surname);
        }

        private static bool IsTitleToken(string token)
        {
            var t = token.Trim().ToLowerInvariant();
            return t is "prof." or "prof" or "doç." or "doç" or "doc." or "doc" or "op." or "op" or "uzm." or "uzm" or "dr." or "dr";
        }

        private static string NormalizeTitle(string title)
        {
            var normalized = title.ToLowerInvariant().Replace("  ", " ").Trim();
            return normalized switch
            {
                "prof. dr." or "prof dr" or "prof. dr" => "Prof. Dr.",
                "doç. dr." or "doç dr" or "doç. dr" or "doc. dr." or "doc dr" or "doc. dr" => "Doç. Dr.",
                "op. dr." or "op dr" or "op. dr" => "Op. Dr.",
                "uzm. dr." or "uzm dr" or "uzm. dr" => "Uzm. Dr.",
                "dr." or "dr" => "Dr.",
                _ => "Uzm. Dr."
            };
        }

        private static string ConvertToIdentifier(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text
                .ToLowerInvariant()
                .Replace("ı", "i")
                .Replace("ö", "o")
                .Replace("ü", "u")
                .Replace("ş", "s")
                .Replace("ç", "c")
                .Replace("ğ", "g")
                .Replace("'", string.Empty)
                .Replace("`", string.Empty)
                .Trim();
        }

        private static string? ResolveSeedPassword(
            string? configuredPassword,
            IHostEnvironment environment,
            string developmentFallback,
            ILogger logger,
            string accountType)
        {
            if (!string.IsNullOrWhiteSpace(configuredPassword))
            {
                return configuredPassword;
            }

            if (environment.IsDevelopment())
            {
                logger.LogWarning(
                    "{AccountType} seed sifresi konfigurasyonda bulunamadi. Gelistirme ortami icin gecici varsayilan kullanilacak.",
                    accountType);
                return developmentFallback;
            }

            logger.LogWarning(
                "{AccountType} seed sifresi konfigurasyonda bulunamadigi icin varsayilan hesap olusturma atlandi.",
                accountType);
            return null;
        }
    }
}
