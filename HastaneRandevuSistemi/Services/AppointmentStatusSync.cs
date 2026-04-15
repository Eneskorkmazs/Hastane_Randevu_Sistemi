using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Services
{
    public static class AppointmentStatusSync
    {
        public static async Task CompleteExpiredAppointmentsAsync(ApplicationDbContext context)
        {
            var now = DateTime.Now;

            var expiredAppointments = await context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.AppointmentDate <= now
                    && a.Status != AppointmentStatus.Iptal
                    && a.Status != AppointmentStatus.Tamamlandi)
                .ToListAsync();

            if (expiredAppointments.Count == 0)
            {
                return;
            }

            foreach (var appointment in expiredAppointments)
            {
                appointment.Status = AppointmentStatus.Tamamlandi;

                if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
                {
                    var doctorName = appointment.Doctor == null
                        ? "doktorunuz"
                        : $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim();
                    var departmentName = appointment.Doctor?.Department?.Name ?? "ilgili bölüm";

                    context.Notifications.Add(new Notification
                    {
                        UserId = appointment.PatientUserId,
                        Title = "Randevunuz tamamlandı",
                        Message = $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli {departmentName} / {doctorName} randevunuz tamamlandı. Geçmiş olsun, sağlıklı günler dileriz.",
                        Type = "Durum",
                        Link = "/Appointment/Index",
                        CreatedDate = now
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}

