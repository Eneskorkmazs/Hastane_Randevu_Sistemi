using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Services
{
    public class AppointmentReminderService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(IServiceProvider serviceProvider, ILogger<AppointmentReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunOnceAsync(stoppingToken);

            using var timer = new PeriodicTimer(CheckInterval);
            while (!stoppingToken.IsCancellationRequested
                   && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
            }
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
                var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();

                var now = DateTime.Now;
                var tomorrowStart = now.Date.AddDays(1);
                var tomorrowEnd = tomorrowStart.AddDays(1);

                var appointments = await context.Appointments
                    .Include(a => a.PatientUser)
                    .Include(a => a.Doctor)
                    .ThenInclude(d => d!.Department)
                    .Where(a =>
                        a.PatientUserId != null &&
                        a.AppointmentDate > now &&
                        a.AppointmentDate >= tomorrowStart &&
                        a.AppointmentDate < tomorrowEnd &&
                        a.Status != AppointmentStatus.Iptal &&
                        a.Status != AppointmentStatus.Tamamlandi &&
                        a.ReminderSentAt == null)
                    .ToListAsync(cancellationToken);

                if (appointments.Count == 0)
                {
                    return;
                }

                var reminderCreatedAt = DateTime.Now;
                var sentCount = 0;
                foreach (var appointment in appointments)
                {
                    var user = appointment.PatientUser;
                    if (user == null)
                    {
                        _logger.LogWarning("Hatirlatma atlandi. Kullanici eksik. AppointmentId: {AppointmentId}", appointment.Id);
                        continue;
                    }

                    var doctorName = appointment.Doctor == null
                        ? "doktorunuz"
                        : $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim();
                    var departmentName = appointment.Doctor?.Department?.Name ?? "ilgili bolum";

                    var title = "Randevu hatirlatmasi";
                    var message = $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli {departmentName} / {doctorName} randevunuz yarin. Lutfen zamaninda hastanede olunuz.";
                    var emailBody = $@"
<div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
    <h2 style='color: #2c3e50;'>Hastane Randevu Sistemi</h2>
    <h3 style='color: #34495e;'>Randevunuz Yarin</h3>
    <p style='font-size: 16px; color: #555;'>{message}</p>
    <p style='font-size: 14px; color: #777;'>Randevu No: {appointment.Id}</p>
    <hr style='border: 0; border-top: 1px solid #ddd; margin: 20px 0;'/>
    <p style='font-size: 12px; color: #aaa;'>Saglikli gunler dileriz.</p>
</div>";

                    var sent = false;
                    if (!string.IsNullOrWhiteSpace(user.Email))
                    {
                        sent = await emailService.SendEmailAsync(user.Email, title, emailBody);
                    }
                    var smsSent = await smsService.SendAppointmentSmsAsync(
                        appointment.PatientPhone ?? user.Telefon ?? user.PhoneNumber,
                        $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli {departmentName} / {doctorName} randevunuz yarin. Lutfen zamaninda hastanede olunuz.");

                    if (!sent && !smsSent)
                    {
                        continue;
                    }

                    context.Notifications.Add(new Notification
                    {
                        UserId = user.Id,
                        Title = title,
                        Message = message,
                        Type = "Hatirlatma",
                        Link = "/Appointment/Index",
                        CreatedDate = reminderCreatedAt
                    });

                    appointment.ReminderSentAt = reminderCreatedAt;
                    sentCount++;
                }

                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Randevu hatirlatma taramasi tamamlandi. En az bir kanaldan iletilen kayit sayisi: {Count}", sentCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AppointmentReminderService calisirken beklenmeyen bir hata olustu.");
            }
        }
    }
}
