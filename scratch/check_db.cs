
using System;
using System.Linq;
using System.Threading.Tasks;
using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HastaneRandevuSistemiDb;Trusted_Connection=True;MultipleActiveResultSets=true"));
    })
    .Build();

using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

var appointmentId = 62;
var app = await db.Appointments.Include(a => a.Doctor).FirstOrDefaultAsync(a => a.Id == appointmentId);

if (app == null) {
    Console.WriteLine($"Appointment {appointmentId} NOT FOUND");
} else {
    Console.WriteLine($"Appointment {appointmentId} found. DoctorId: {app.DoctorId}, Doctor: {app.Doctor?.Name} {app.Doctor?.Surname}, Status: {app.Status}");
}

var doctors = await db.Doctors.ToListAsync();
foreach(var d in doctors) {
    Console.WriteLine($"Doctor: {d.Id}, Name: {d.Name} {d.Surname}, UserId: {d.UserId}");
}
