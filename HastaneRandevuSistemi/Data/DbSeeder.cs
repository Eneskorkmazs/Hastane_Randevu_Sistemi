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

            await context.Database.MigrateAsync();

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

            if (await context.Departments.AnyAsync())
            {
                return;
            }

            var hospitalData = new Dictionary<string, List<string>>
            {
                { "Dahiliye (Ic Hastaliklari)", new List<string> { "Prof. Dr. Canan Karatay", "Uzm. Dr. Ahmet Maranki" } },
                { "Kardiyoloji", new List<string> { "Prof. Dr. Mehmet Oz", "Doc. Dr. Bingur Sonmez" } },
                { "Noroloji", new List<string> { "Prof. Dr. Gazi Yasargil", "Uzm. Dr. Serdar Dag" } },
                { "Ortopedi ve Travmatoloji", new List<string> { "Op. Dr. Feridun Kunak", "Prof. Dr. Burhan Uslu" } },
                { "Goz Hastaliklari", new List<string> { "Op. Dr. Kudret Goz", "Uzm. Dr. Levent Akcay" } },
                { "Kulak Burun Bogaz", new List<string> { "Op. Dr. Aytug Altundag", "Prof. Dr. Ibrahim Saracoglu" } },
                { "Genel Cerrahi", new List<string> { "Prof. Dr. Munci Kalayoglu", "Op. Dr. Ender Sarac" } },
                { "Dermatoloji", new List<string> { "Uzm. Dr. Nihat Hatipoglu", "Dr. Seyma Subasi" } },
                { "Pediatri", new List<string> { "Uzm. Dr. Osman Muftuoglu", "Dr. Sami Ulus" } },
                { "Psikiyatri", new List<string> { "Prof. Dr. Ilber Ortayli", "Dr. Gulseren Budayicioglu" } },
                { "Uroloji", new List<string> { "Op. Dr. Haydar Dumen", "Prof. Dr. Kemal Ozkan" } },
                { "Fizik Tedavi ve Rehabilitasyon", new List<string> { "Uzm. Dr. Halit Yerebakan", "Dr. Ferhat Gocer" } },
                { "Kadin Hastaliklari ve Dogum", new List<string> { "Op. Dr. Banu Ciftci", "Prof. Dr. Teksen Camlibel" } },
                { "Gogus Hastaliklari", new List<string> { "Prof. Dr. Ahmet Rasim Kucukusta", "Uzm. Dr. Tevfik Ozlu" } },
                { "Enfeksiyon Hastaliklari", new List<string> { "Prof. Dr. Mehmet Ceyhan", "Doc. Dr. Ates Kara" } }
            };

            foreach (var item in hospitalData)
            {
                var department = new Department { Name = item.Key };
                await context.Departments.AddAsync(department);
                await context.SaveChangesAsync();

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
