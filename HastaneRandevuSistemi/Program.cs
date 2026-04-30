using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Debug()
    .WriteTo.File(
        Path.Combine("Logs", "hrs-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"]))
{
    builder.WebHost.UseUrls("http://localhost:5087");
}

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .WriteTo.Console()
        .WriteTo.Debug()
        .WriteTo.File(
            Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs", "hrs-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true);
}, preserveStaticLogger: true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var localConnectionString = builder.Configuration.GetConnectionString("LocalDefaultConnection")
    ?? "Data Source=HastaneRandevuSistemi.local.db";
var databaseProvider = builder.Configuration["DatabaseProvider"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HastaneRandevuSistemi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HastaneRandevuSistemi";
var jwtSecret = builder.Configuration["Jwt:Key"] ?? "HastaneRandevuSistemiSuperSecretKeyDahaUzunOlmali123!";

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
builder.Services.AddScoped<HastaneRandevuSistemi.Services.ISymptomCheckerService, HastaneRandevuSistemi.Services.SymptomCheckerService>();
builder.Services.AddHttpClient<HastaneRandevuSistemi.Services.SmsService>();
builder.Services.AddHostedService<HastaneRandevuSistemi.Services.AppointmentReminderService>();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024;
    options.SizeLimit = 1024 * 1024 * 10;
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__HRS-CSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        || builder.Environment.IsEnvironment("Testing")
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterViewModelValidator>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        || builder.Environment.IsEnvironment("Testing")
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hastane Randevu Sistemi API V1");
    });
}

app.UseStaticFiles();
app.UseResponseCaching();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com data:; img-src 'self' data: https://www.transparenttextures.com;";
    await next();
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (!app.Environment.IsEnvironment("Testing"))
{
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
}

app.Run();
Log.CloseAndFlush();

public partial class Program { }
