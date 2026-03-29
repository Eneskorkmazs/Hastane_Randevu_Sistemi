using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"]))
{
    builder.WebHost.UseUrls("http://localhost:5087");
}

// Varsayilan EventLog provider'i bu ortamda yetki hatasi uretebildigi icin
// explicit olarak guvenli provider'lara dusuyoruz.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var localConnectionString = builder.Configuration.GetConnectionString("LocalDefaultConnection")
    ?? "Data Source=HastaneRandevuSistemi.local.db";
var databaseProvider = builder.Configuration["DatabaseProvider"];

if (string.IsNullOrWhiteSpace(databaseProvider))
{
    databaseProvider = string.IsNullOrWhiteSpace(connectionString) ? "Sqlite" : "Postgres";
}

if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
    && string.IsNullOrWhiteSpace(connectionString)
    && !string.IsNullOrWhiteSpace(localConnectionString))
{
    // Yerel calismada Supabase kapali olsa bile uygulamayi dusurmeyelim.
    databaseProvider = "Sqlite";
}

if ((databaseProvider.Equals("LocalDb", StringComparison.OrdinalIgnoreCase)
    || databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    && string.IsNullOrWhiteSpace(localConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:LocalDefaultConnection ayari bos. Local baglanti bilgisini appsettings.Development.json icinde tanimlayin.");
}

if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
    && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection ayari bos. Supabase PostgreSQL baglanti bilgisini appsettings veya environment uzerinden girin.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider.Equals("LocalDb", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(localConnectionString);
    }
    else if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(localConnectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<HastaneRandevuSistemi.Services.EmailService>();
builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
    .SetApplicationName("HastaneRandevuSistemi");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.Seed(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabani tohumlanirken bir hata olustu.");
    }
}

app.Run();
