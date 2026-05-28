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

            var model = new HastaneRandevuSistemi.ViewModels.SecretaryDashboardViewModel();

            model.PendingAppointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.Status == AppointmentStatus.Bekliyor)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            model.UpcomingNext2Hours = await _context.Appointments
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

            model.UnsentPrescriptions = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.PrescriptionCreatedAt != null && a.PrescriptionSentAt == null)
                .OrderByDescending(a => a.PrescriptionCreatedAt)
                .ToListAsync();

            model.SentPrescriptions = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.PrescriptionSentAt != null)
                .OrderByDescending(a => a.PrescriptionSentAt)
                .Take(20)
                .ToListAsync();

            model.RecentlyCancelled = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .Where(a => a.Status == AppointmentStatus.Iptal && a.AppointmentDate >= today.AddDays(-1))
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .ToListAsync();

            // Stats
            model.TodaysTotal = todaysAppointments.Count;
            model.TodaysCompleted = todaysAppointments.Count(a => a.Status == AppointmentStatus.Tamamlandi);
            model.UrgentApprovals = model.PendingAppointments.Count(a => a.AppointmentDate < tomorrow);
            model.TomorrowsTotal = await _context.Appointments
                .CountAsync(a => a.AppointmentDate >= tomorrow && a.AppointmentDate < tomorrow.AddDays(1) && a.Status != AppointmentStatus.Iptal);
            model.OldPendingCount = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Bekliyor && a.CreatedDate < now.AddDays(-2));
            model.DelayedPrescriptions = await _context.Appointments
                .CountAsync(a => a.Status == AppointmentStatus.Tamamlandi && a.PrescriptionCreatedAt == null && a.AppointmentDate < today);

            // Department Load (Busiest)
            model.DepartmentLoads = await _context.Appointments
                .Where(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow && a.Status != AppointmentStatus.Iptal)
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .GroupBy(a => a.Doctor!.Department!.Name)
                .Select(g => new HastaneRandevuSistemi.ViewModels.DepartmentLoadItem
                {
                    DepartmentName = g.Key,
                    AppointmentCount = g.Count(),
                    Capacity = 30 // Varsayılan poliklinik kapasitesi
                })
                .OrderByDescending(x => x.AppointmentCount)
                .Take(5)
                .ToListAsync();

            var secretaryUser = await _userManager.GetUserAsync(User);

            model.SystemAnnouncements = secretaryUser == null
                ? new List<Notification>()
                : await _context.Notifications
                    .Where(n => n.UserId == secretaryUser.Id && (n.Type == "Duyuru" || n.Type == "DuyuruSekreter"))
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(5)
                    .ToListAsync();

            model.Pharmacies = await _context.Pharmacies.OrderBy(p => p.Name).ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchPatients(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { success = false, message = "Sorgu boş olamaz." });

            var queryable = _context.Appointments.AsNoTracking();
            var normalizedQuery = query.Trim().ToLower();

            if (normalizedQuery == "today")
            {
                var today = DateTime.Today;
                queryable = queryable.Where(a => a.AppointmentDate >= today && a.AppointmentDate < today.AddDays(1) && a.Status != AppointmentStatus.Iptal);
            }
            else
            {
                if (normalizedQuery.Length < 3)
                    return Json(new { success = false, message = "En az 3 karakter giriniz." });

                queryable = queryable.Where(a => (a.PatientName ?? string.Empty).ToLower().Contains(normalizedQuery) || 
                                              (a.PatientSurname ?? string.Empty).ToLower().Contains(normalizedQuery) || 
                                              (a.PatientPhone ?? string.Empty).Contains(normalizedQuery));
            }

            var results = await queryable
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new {
                    id = a.Id,
                    name = a.PatientName + " " + a.PatientSurname,
                    phone = a.PatientPhone,
                    date = a.AppointmentDate.ToString("dd.MM.yyyy HH:mm"),
                    status = a.Status.ToString(),
                    doctor = a.Doctor != null ? a.Doctor.Name + " " + a.Doctor.Surname : "Belirtilmemiş",
                    department = (a.Doctor != null && a.Doctor.Department != null) ? a.Doctor.Department.Name : "Belirtilmemiş"
                })
                .Take(20)
                .ToListAsync();

            return Json(new { success = true, data = results });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(string Title, string Message)
        {
            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message))
                return Json(new { success = false, message = "Başlık ve mesaj zorunludur." });

            var patients = await _userManager.GetUsersInRoleAsync("Hasta");
            if (patients.Count == 0)
            {
                return Json(new { success = false, message = "Duyuru gönderilecek hasta kullanıcısı bulunamadı." });
            }

            var createdDate = DateTime.Now;
            await _context.Notifications.AddRangeAsync(patients.Select(patient => new Notification
            {
                UserId = patient.Id,
                Title = Title.Trim(),
                Message = Message.Trim(),
                Type = "DuyuruHasta",
                CreatedDate = createdDate,
                Link = null
            }));
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Duyuru başarıyla yayınlandı.";
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> PendingAppointmentCount()
        {
            var count = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Bekliyor);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> UnreadNotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { count = 0 });

            var announcementCount = await _context.Notifications.CountAsync(n =>
                n.UserId == user.Id && (n.Type == "Duyuru" || n.Type == "DuyuruSekreter") && !n.IsRead);
            var pendingAppointmentCount = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Bekliyor);

            return Json(new { count = announcementCount + pendingAppointmentCount });
        }

        [HttpGet]
        public async Task<IActionResult> PendingPrescriptionCount()
        {
            var count = await _context.Appointments
                .CountAsync(a => a.PrescriptionCreatedAt != null && a.PrescriptionSentAt == null);

            return Json(new { count });
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
        public async Task<IActionResult> SendPrescription(int id, int? pharmacyId)
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

            var actor = await GetCurrentActorAsync();
            appointment.PrescriptionSentAt = DateTime.Now;
            appointment.PrescriptionSentByName = actor.DisplayName + " (Sekreter)";

            Pharmacy? pharmacy = null;
            if (pharmacyId.HasValue)
            {
                pharmacy = await _context.Pharmacies.FindAsync(pharmacyId.Value);
                if (pharmacy != null)
                {
                    appointment.PharmacyId = pharmacy.Id;
                    appointment.PharmacyStatus = PrescriptionPharmacyStatus.Bekliyor;
                }
            }

            await _context.SaveChangesAsync();

            var doctorName = appointment.Doctor == null
                ? "doktorunuz"
                : $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim();

            var messageBody =
                $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli muayeneniz icin {doctorName} tarafindan yazilan recete tarafiniza iletildi. " +
                $"Tani: {appointment.PrescriptionDiagnosis}. Ilaclar: {appointment.PrescriptionMedications}.";

            if (pharmacy != null)
            {
                messageBody += $" Ayrıca reçeteniz {pharmacy.Name} ({pharmacy.District}) isimli eczaneye iletilmiştir.";
            }

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Receteniz hazir" + (pharmacy != null ? " ve Eczaneye İletildi" : ""),
                    messageBody,
                    "Recete",
                    "/Patient/MedicalHistory#receteler");
            }

            TempData["SuccessMessage"] = pharmacy != null ? $"Reçete {pharmacy.Name} eczanesine ve hastaya gönderildi." : "Recete hastaya gonderildi.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true });
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAllPrescriptions()
        {
            var unsent = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PrescriptionCreatedAt != null && a.PrescriptionSentAt == null)
                .ToListAsync();

            if (!unsent.Any())
            {
                TempData["InfoMessage"] = "Gönderilecek yeni reçete bulunamadı.";
                return RedirectToAction(nameof(Dashboard));
            }

            var actor = await GetCurrentActorAsync();
            var now = DateTime.Now;
            int count = 0;

            foreach (var app in unsent)
            {
                app.PrescriptionSentAt = now;
                app.PrescriptionSentByName = actor.DisplayName + " (Sekreter)";
                
                var doctorName = app.Doctor == null
                    ? "doktorunuz"
                    : $"{app.Doctor.Title} {app.Doctor.Name} {app.Doctor.Surname}".Trim();

                var messageBody =
                    $"{app.AppointmentDate:dd.MM.yyyy HH:mm} tarihli muayeneniz icin {doctorName} tarafindan yazilan recete tarafiniza iletildi. " +
                    $"Tani: {app.PrescriptionDiagnosis}. Ilaclar: {app.PrescriptionMedications}.";

                if (!string.IsNullOrWhiteSpace(app.PatientUserId))
                {
                    await CreateNotificationAsync(
                        app.PatientUserId,
                        "Receteniz hazir",
                        messageBody,
                        "Recete",
                        "/Patient/MedicalHistory#receteler");
                }
                
                count++;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{count} adet reçete başarıyla gönderildi.";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, count });
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
        [HttpGet]
        public async Task<IActionResult> GetPatientDetails(int appointmentId)
        {
            var initialAppointment = await _context.Appointments.FindAsync(appointmentId);
            if (initialAppointment == null) return Json(new { success = false });

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => (a.PatientName == initialAppointment.PatientName && a.PatientSurname == initialAppointment.PatientSurname) || 
                            (a.PatientPhone == initialAppointment.PatientPhone))
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new {
                    id = a.Id,
                    date = a.AppointmentDate.ToString("dd.MM.yyyy HH:mm"),
                    doctor = a.Doctor != null ? a.Doctor.Title + " " + a.Doctor.Surname : "Bilinmiyor",
                    department = (a.Doctor != null && a.Doctor.Department != null) ? a.Doctor.Department.Name : "Bilinmiyor",
                    status = a.Status.ToString(),
                    canCancel = a.Status != AppointmentStatus.Iptal && a.Status != AppointmentStatus.Tamamlandi
                })
                .ToListAsync();

            return Json(new { 
                success = true, 
                patientName = initialAppointment.PatientName + " " + initialAppointment.PatientSurname,
                phone = initialAppointment.PatientPhone,
                history = appointments 
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSearchAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return Json(new { success = false, message = "Randevu bulunamadı." });

            if (appointment.Status is AppointmentStatus.Iptal or AppointmentStatus.Tamamlandi)
                return Json(new { success = false, message = "Bu randevu iptal edilemez." });

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
                    "Randevunuz iptal edildi",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz sekreter tarafindan iptal edildi.",
                    "Durum",
                    "/Appointment/Index");
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> ExportDailyList()
        {
            var today = DateTime.Today;
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.AppointmentDate >= today && a.AppointmentDate < today.AddDays(1) && a.Status != AppointmentStatus.Iptal)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var pdfBytes = SimplePdfGenerator.CreateDailyListPdf($"Tarih: {today:dd.MM.yyyy}", appointments);
            return File(pdfBytes, "application/pdf", $"Gunluk_Liste_{today:dd_MM_yyyy}.pdf");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Notifications.FindAsync(id);
            if (announcement == null) return Json(new { success = false, message = "Duyuru bulunamadı." });

            _context.Notifications.Remove(announcement);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
