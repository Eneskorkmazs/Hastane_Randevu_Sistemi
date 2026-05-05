using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Controllers
{
    [Authorize(Roles = "Sekreter")]
    public class SecretaryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;

        public SecretaryController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            var pendingAppointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.Status == AppointmentStatus.Bekliyor && a.AppointmentDate >= today)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var upcomingNext2Hours = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.AppointmentDate > now && a.AppointmentDate <= now.AddHours(2) && a.Status == AppointmentStatus.Onaylandi)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var todaysAppointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow && a.Status != AppointmentStatus.Iptal)
                .ToListAsync();

            var unsentPrescriptions = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.PrescriptionCreatedAt != null && a.PrescriptionSentAt == null)
                .OrderByDescending(a => a.PrescriptionCreatedAt)
                .ToListAsync();

            var appointmentsForPrescription = new List<Appointment>(); // Reçete artık doktor tarafından yazılıyor

            var sentPrescriptions = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.PrescriptionSentAt != null)
                .OrderByDescending(a => a.PrescriptionSentAt)
                .Take(30)
                .ToListAsync();

            ViewBag.PendingAppointments = pendingAppointments;
            ViewBag.UnsentPrescriptions = unsentPrescriptions;
            ViewBag.AppointmentsForPrescription = appointmentsForPrescription;
            ViewBag.SentPrescriptions = sentPrescriptions;
            
            // Reminders & Stats
            ViewBag.UpcomingNext2Hours = upcomingNext2Hours;
            ViewBag.TodaysTotal = todaysAppointments.Count;
            ViewBag.TodaysCompleted = todaysAppointments.Count(a => a.Status == AppointmentStatus.Tamamlandi);
            ViewBag.UrgentApprovals = pendingAppointments.Count(a => a.AppointmentDate < tomorrow);
            ViewBag.DelayedPrescriptions = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Tamamlandi && a.PrescriptionCreatedAt == null && a.AppointmentDate < today);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (appointment.Status == AppointmentStatus.Tamamlandi)
            {
                TempData["InfoMessage"] = "Tamamlanmis randevular tekrar onaylanamaz.";
                return RedirectToAction(nameof(Dashboard));
            }

            var actor = await GetCurrentActorAsync();
            appointment.Status = AppointmentStatus.Onaylandi;
            appointment.ApprovedByUserId = actor.UserId;
            appointment.ApprovedByName = actor.DisplayName + " (Sekreter)";
            appointment.ApprovedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevunuz onaylandi",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz sekreter tarafindan onaylandi.",
                    "Durum",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Randevu onaylandi.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (appointment.Status is AppointmentStatus.Iptal or AppointmentStatus.Tamamlandi)
            {
                TempData["InfoMessage"] = "Bu randevu reddedilemez.";
                return RedirectToAction(nameof(Dashboard));
            }

            var actor = await GetCurrentActorAsync();
            appointment.Status = AppointmentStatus.Iptal;
            appointment.CancelledByUserId = actor.UserId;
            appointment.CancelledByName = actor.DisplayName + " (Sekreter)";
            appointment.CancelledDate = DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevunuz reddedildi",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz sekreter tarafindan iptal edildi.",
                    "Durum",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Randevu reddedildi.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPrescription(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Reçete bulunamadı.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (appointment.PrescriptionCreatedAt == null)
            {
                TempData["ErrorMessage"] = "Bu randevu icin henuz recete yazilmamis.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                TempData["ErrorMessage"] = "Bu randevuya bagli bir hasta kullanicisi bulunmadigi icin recete gonderilemedi.";
                return RedirectToAction(nameof(Dashboard));
            }

            var actor = await GetCurrentActorAsync();
            appointment.PrescriptionSentAt = DateTime.Now;
            appointment.PrescriptionSentByName = actor.DisplayName + " (Sekreter)";
            await _context.SaveChangesAsync();

            var doctorName = appointment.Doctor == null
                ? "doktorunuz"
                : $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim();

            var messageBody =
                $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli muayeneniz icin {doctorName} tarafindan yazilan recete tarafiniza iletildi. " +
                $"Tani: {appointment.PrescriptionDiagnosis}. Ilaclar: {appointment.PrescriptionMedications}.";

            await CreateNotificationAsync(
                appointment.PatientUserId,
                "Receteniz hazir",
                messageBody,
                "Recete",
                "/Patient/MedicalHistory#receteler");

            TempData["SuccessMessage"] = "Recete hastaya gonderildi.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Prescription(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Tarihi gelmemiş randevulara reçete yazılamaz
            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Tarihi gelmemiş bir randevu için reçete oluşturulamaz.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Sadece tamamlanmış randevulara reçete yazılabilir
            if (appointment.Status != AppointmentStatus.Tamamlandi)
            {
                TempData["ErrorMessage"] = "Reçete yalnızca tamamlanmış randevular için yazılabilir.";
                return RedirectToAction(nameof(Dashboard));
            }

            var model = new HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel
            {
                AppointmentId = appointment.Id,
                PatientName = appointment.PatientName,
                PatientSurname = appointment.PatientSurname,
                DoctorName = appointment.Doctor != null ? $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim() : "Bilinmiyor",
                DepartmentName = appointment.Doctor?.Department?.Name ?? string.Empty,
                PrescriptionDate = appointment.PrescriptionCreatedAt ?? DateTime.Now,
                Diagnosis = appointment.PrescriptionDiagnosis ?? string.Empty,
                Medications = appointment.PrescriptionMedications ?? string.Empty,
                Notes = appointment.PrescriptionNotes
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPrescriptionPdf(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.PrescriptionCreatedAt == null || string.IsNullOrWhiteSpace(appointment.PrescriptionMedications))
            {
                TempData["ErrorMessage"] = "PDF oluşturmak için önce reçete kaydı hazırlanmalıdır.";
                return RedirectToAction(nameof(Prescription), new { appointmentId });
            }

            var model = BuildPrescriptionModel(appointment);
            var pdf = SimplePdfGenerator.CreatePrescriptionPdf(model);
            return File(pdf, "application/pdf", $"recete-{appointment.Id}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prescription(HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel model)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Tarihi gelmemiş randevulara reçete yazılamaz
            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Tarihi gelmemiş bir randevu için reçete oluşturulamaz.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Sadece tamamlanmış randevulara reçete yazılabilir
            if (appointment.Status != AppointmentStatus.Tamamlandi)
            {
                TempData["ErrorMessage"] = "Reçete yalnızca tamamlanmış randevular için yazılabilir.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (!ModelState.IsValid)
            {
                model.DoctorName = appointment.Doctor != null ? $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim() : "Bilinmiyor";
                model.DepartmentName = appointment.Doctor?.Department?.Name ?? string.Empty;
                model.PatientName = appointment.PatientName;
                model.PatientSurname = appointment.PatientSurname;
                return View(model);
            }

            appointment.PrescriptionDiagnosis = model.Diagnosis.Trim();
            appointment.PrescriptionMedications = model.Medications.Trim();
            appointment.PrescriptionNotes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
            appointment.PrescriptionCreatedAt = DateTime.Now;
            appointment.PrescriptionSentAt = null;
            appointment.PrescriptionSentByName = null;
            
            await _context.SaveChangesAsync();

            var cache = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache)) as Microsoft.Extensions.Caching.Memory.IMemoryCache;
            if (cache != null && !string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                cache.Remove($"patient:prescriptions:{appointment.PatientUserId}");
            }

            TempData["SuccessMessage"] = "Recete kaydedildi. Gonderilmemis receteler listesinden hastaya iletebilirsiniz.";
            model.DoctorName = appointment.Doctor != null ? $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim() : "Bilinmiyor";
            model.DepartmentName = appointment.Doctor?.Department?.Name ?? string.Empty;
            model.PatientName = appointment.PatientName;
            model.PatientSurname = appointment.PatientSurname;
            model.PrescriptionDate = appointment.PrescriptionCreatedAt.Value;
            
            return View("PrescriptionPreview", model);
        }

        private static HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel BuildPrescriptionModel(Appointment appointment)
        {
            return new HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel
            {
                AppointmentId = appointment.Id,
                PatientName = appointment.PatientName,
                PatientSurname = appointment.PatientSurname,
                DoctorName = appointment.Doctor == null
                    ? "Bilinmeyen Doktor"
                    : $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim(),
                DepartmentName = appointment.Doctor?.Department?.Name ?? string.Empty,
                PrescriptionDate = appointment.PrescriptionCreatedAt ?? DateTime.Now,
                Diagnosis = appointment.PrescriptionDiagnosis ?? string.Empty,
                Medications = appointment.PrescriptionMedications ?? string.Empty,
                Notes = appointment.PrescriptionNotes
            };
        }

        private async Task<(string? UserId, string DisplayName)> GetCurrentActorAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var displayName = user != null
                ? $"{user.Name} {user.Surname}".Trim()
                : (User.Identity?.Name ?? "Sekreter");
            return (user?.Id, displayName);
        }

        private async Task CreateNotificationAsync(string userId, string title, string message, string type, string link)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    var emailBody = $@"
<div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
    <h2 style='color: #2c3e50;'>Hastane Randevu Sistemi</h2>
    <h3 style='color: #34495e;'>{title}</h3>
    <p style='font-size: 16px; color: #555;'>{message}</p>
    <hr style='border: 0; border-top: 1px solid #ddd; margin: 20px 0;'/>
    <p style='font-size: 12px; color: #aaa;'>Saglikli gunler dileriz.</p>
</div>";
                    await _emailService.SendEmailAsync(user.Email, title, emailBody);
                }
            }
            catch
            {
                // mail gonderimi basarisiz olursa akisi kirmayiz
            }
        }
    }
}

