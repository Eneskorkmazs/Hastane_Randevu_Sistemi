using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Postgres";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var localConnectionString = builder.Configuration.GetConnectionString("LocalDefaultConnection");

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
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<HastaneRandevuSistemi.Services.EmailService>();
builder.Services.AddControllersWithViews();

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
