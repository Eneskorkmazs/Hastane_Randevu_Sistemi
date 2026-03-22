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

            var provider = context.Database.ProviderName ?? string.Empty;
            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            string[] roles = { "Admin", "Doktor", "Hasta" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            const string adminEmail = "admin@havatakip.com.tr";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var newAdmin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Sistem",
                    Surname = "Yoneticisi",
                    EmailConfirmed = true
                };

                var createAdminResult = await userManager.CreateAsync(newAdmin, "Admin123!");
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            var renameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Dahiliye (Ic Hastaliklari)", "Dahiliye (İç Hastalıkları)" },
                { "Dis Hastaliklari", "Diş Hastalıkları" },
                { "Noroloji", "Nöroloji" },
                { "Goz Hastaliklari", "Göz Hastalıkları" },
                { "Kulak Burun Bogaz", "Kulak Burun Boğaz" },
                { "Kadin Hastaliklari ve Dogum", "Kadın Hastalıkları ve Doğum" },
                { "Gogus Hastaliklari", "Göğüs Hastalıkları" },
                { "Enfeksiyon Hastaliklari", "Enfeksiyon Hastalıkları" },
                { "Uroloji", "Üroloji" }
            };

            // Var olan kayitlarin isimlerini duzelt
            var existingDepartments = await context.Departments.ToListAsync();
            foreach (var dep in existingDepartments)
            {
                if (renameMap.TryGetValue(dep.Name, out var newName))
                {
                    dep.Name = newName;
                }
            }
            if (existingDepartments.Any())
            {
                await context.SaveChangesAsync();
            }

            if (existingDepartments.Any())
            {
                // Var olanlar duzeltildi; eksik olanlari eklemeye devam edebiliriz.
            }

            var hospitalData = new Dictionary<string, List<string>>
            {
                { "Diş Hastalıkları", new List<string> { "Uzm. Dr. Elif Yılmaz" } },
                { "Dahiliye (İç Hastalıkları)", new List<string> { "Prof. Dr. Canan Karatay", "Uzm. Dr. Ahmet Maranki" } },
                { "Kardiyoloji", new List<string> { "Prof. Dr. Mehmet Öz", "Doç. Dr. Bingür Sönmez" } },
                { "Nöroloji", new List<string> { "Prof. Dr. Gazi Yaşargil", "Uzm. Dr. Serdar Dağ" } },
                { "Ortopedi ve Travmatoloji", new List<string> { "Op. Dr. Feridun Kunak", "Prof. Dr. Burhan Uslu" } },
                { "Göz Hastalıkları", new List<string> { "Op. Dr. Kudret Göz", "Uzm. Dr. Levent Akçay" } },
                { "Kulak Burun Boğaz", new List<string> { "Op. Dr. Aytuğ Altundağ", "Prof. Dr. İbrahim Saraçoğlu" } },
                { "Genel Cerrahi", new List<string> { "Prof. Dr. Münci Kalayoğlu", "Op. Dr. Ender Saraç" } },
                { "Dermatoloji", new List<string> { "Uzm. Dr. Nihat Hatipoğlu", "Dr. Şeyma Subaşı" } },
                { "Pediatri", new List<string> { "Uzm. Dr. Osman Müftüoğlu", "Dr. Sami Ulus" } },
                { "Psikiyatri", new List<string> { "Prof. Dr. İlber Ortaylı", "Dr. Gülseren Budayıcıoğlu" } },
                { "Üroloji", new List<string> { "Op. Dr. Haydar Dümen", "Prof. Dr. Kemal Özkan" } },
                { "Fizik Tedavi ve Rehabilitasyon", new List<string> { "Uzm. Dr. Halit Yerebakan", "Dr. Ferhat Göçer" } },
                { "Kadın Hastalıkları ve Doğum", new List<string> { "Op. Dr. Banu Çiftçi", "Prof. Dr. Teksen Çamlıbel" } },
                { "Göğüs Hastalıkları", new List<string> { "Prof. Dr. Ahmet Rasim Küçükusta", "Uzm. Dr. Tevfik Özlü" } },
                { "Enfeksiyon Hastalıkları", new List<string> { "Prof. Dr. Mehmet Ceyhan", "Doç. Dr. Ateş Kara" } }
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

                foreach (var doctorFullName in item.Value)
                {
                    var parts = doctorFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var surname = parts.Last();
                    var name = string.Join(" ", parts.Take(parts.Length - 1));

                    var cleanName = ConvertToIdentifier(name)
                        .Replace("prof.", "")
                        .Replace("dr.", "")
                        .Replace("uzm.", "")
                        .Replace("doc.", "")
                        .Replace("op.", "")
                        .Trim()
                        .Replace(" ", ".");

                    var cleanSurname = ConvertToIdentifier(surname);
                    var email = $"{cleanName}.{cleanSurname}@havatakip.com.tr".ToLowerInvariant();

                    if (await userManager.FindByEmailAsync(email) != null)
                    {
                        continue;
                    }

                    var user = new AppUser
                    {
                        UserName = email,
                        Email = email,
                        Name = name,
                        Surname = surname,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, "Doktor123!");
                    if (!result.Succeeded)
                    {
                        continue;
                    }

                    await userManager.AddToRoleAsync(user, "Doktor");

                    await context.Doctors.AddAsync(new Doctor
                    {
                        Name = name,
                        Surname = surname,
                        DepartmentId = department.Id,
                        UserId = user.Id
                    });
                }
            }

            await context.SaveChangesAsync();
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
                .Replace("ğ", "g");
        }
    }
}
