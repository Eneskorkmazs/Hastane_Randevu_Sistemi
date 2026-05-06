using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HastaneRandevuSistemi.Services;

namespace HastaneRandevuSistemi.Controllers
{
    [Authorize(Roles = "Hasta")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ISymptomCheckerService _symptomCheckerService;

        public PatientController(ApplicationDbContext context, UserManager<AppUser> userManager, ISymptomCheckerService symptomCheckerService)
        {
            _context = context;
            _userManager = userManager;
            _symptomCheckerService = symptomCheckerService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.PatientUserId == user.Id)
                .ToListAsync();

            var pendingAppointments = appointments.Where(a => a.Status != AppointmentStatus.Tamamlandi && a.Status != AppointmentStatus.Iptal).ToList();
            
            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id || n.Type == "Duyuru")
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToListAsync();

            var prescriptionCount = appointments.Count(a => a.PrescriptionCreatedAt != null);
            var medicalHistoryCount = await _context.MedicalHistories.CountAsync(m => m.UserId == user.Id);
            var unreadNotificationsCount = await _context.Notifications.CountAsync(n => (n.UserId == user.Id || n.Type == "Duyuru") && !n.IsRead);

            var departmentFees = await _context.Departments
                .Select(d => new DepartmentFeeItem { DepartmentName = d.Name, Fee = 150.00m }) 
                .ToListAsync();

            var model = new PatientDashboardViewModel
            {
                FullName = $"{user.Name} {user.Surname}",
                Email = user.Email,
                Telefon = user.Telefon,
                TC = user.TC,
                DogumTarihi = user.DogumTarihi,
                Cinsiyet = user.Cinsiyet,
                BloodType = user.BloodType,
                Allergies = user.Allergies,
                EmergencyContact = user.EmergencyContact,
                PendingAppointmentsCount = pendingAppointments.Count,
                CompletedAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Tamamlandi),
                CancelledAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Iptal),
                PrescriptionCount = prescriptionCount,
                UnreadNotificationsCount = unreadNotificationsCount,
                MedicalHistoryCount = medicalHistoryCount,
                DepartmentFees = departmentFees,
                PendingAppointments = pendingAppointments,
                RecentAppointments = appointments.OrderByDescending(a => a.AppointmentDate).Take(5).ToList(),
                RecentNotifications = notifications
            };

            return View(model);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = new PatientProfileViewModel
            {
                Name = user.Name ?? string.Empty,
                Surname = user.Surname ?? string.Empty,
                TC = user.TC ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Telefon = user.Telefon ?? string.Empty,
                DogumTarihi = user.DogumTarihi,
                Cinsiyet = user.Cinsiyet ?? string.Empty,
                Adres = user.Adres ?? string.Empty,
                BloodType = user.BloodType,
                Allergies = user.Allergies,
                EmergencyContact = user.EmergencyContact,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(PatientProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.Name = model.Name;
            user.Surname = model.Surname;
            user.TC = model.TC;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Telefon = model.Telefon;
            user.DogumTarihi = model.DogumTarihi;
            user.Cinsiyet = model.Cinsiyet;
            user.Adres = model.Adres;
            user.BloodType = model.BloodType;
            user.Allergies = model.Allergies;
            user.EmergencyContact = model.EmergencyContact;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";
                return RedirectToAction(nameof(Dashboard));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        public async Task<IActionResult> MedicalHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var records = await _context.MedicalHistories
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.VisitDate)
                .ToListAsync();

            var prescriptions = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.PatientUserId == user.Id && a.PrescriptionCreatedAt != null)
                .OrderByDescending(a => a.PrescriptionCreatedAt)
                .Select(a => new PatientPrescriptionItemViewModel
                {
                    AppointmentId = a.Id,
                    DoctorName = $"Dr. {a.Doctor!.Name} {a.Doctor.Surname}",
                    DepartmentName = a.Doctor.Department!.Name,
                    PrescriptionDate = a.PrescriptionCreatedAt!.Value,
                    Diagnosis = a.PrescriptionDiagnosis ?? "-",
                    Medications = a.PrescriptionMedications ?? "-",
                    Notes = a.PrescriptionNotes
                })
                .ToListAsync();

            var model = new PatientMedicalHistoryViewModel
            {
                Records = records,
                Prescriptions = prescriptions
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicalHistory(PatientMedicalHistoryViewModel model, IFormFile? attachmentFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            string? attachmentPath = null;
            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                if (!IsValidFile(attachmentFile))
                {
                    TempData["ErrorMessage"] = "Geçersiz dosya formatı. Sadece PDF, JPG ve PNG yükleyebilirsiniz.";
                    return RedirectToAction(nameof(MedicalHistory));
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(attachmentFile.FileName)}";
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "medical_history");
                
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachmentFile.CopyToAsync(stream);
                }
                attachmentPath = $"/uploads/medical_history/{fileName}";
            }

            var record = new MedicalHistory
            {
                UserId = user.Id,
                Title = model.Title,
                VisitDate = model.VisitDate,
                Diagnosis = model.Diagnosis,
                Medications = model.Medications,
                Notes = model.Notes,
                AttachmentPath = attachmentPath,
                CreatedAt = DateTime.Now
            };

            _context.MedicalHistories.Add(record);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tıbbi kayıt başarıyla eklendi.";
            return RedirectToAction(nameof(MedicalHistory));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedicalHistory(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var record = await _context.MedicalHistories.FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);
            if (record != null)
            {
                if (!string.IsNullOrEmpty(record.AttachmentPath))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", record.AttachmentPath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                }
                _context.MedicalHistories.Remove(record);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kayıt başarıyla silindi.";
            }

            return RedirectToAction(nameof(MedicalHistory));
        }

        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadNotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { count = 0 });

            var count = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReadAjax(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
            return Json(new { success = true, unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsReadAjax()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var unread = await _context.Notifications.Where(n => n.UserId == user.Id && !n.IsRead).ToListAsync();
            if (unread.Any())
            {
                unread.ForEach(n => n.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, unreadCount = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotificationAjax(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var notifications = await _context.Notifications.Where(n => n.UserId == user.Id).ToListAsync();
            if (notifications.Any())
            {
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Tüm bildirimler silindi.";
            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(int[] ids)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (ids != null && ids.Length > 0)
            {
                var toDelete = await _context.Notifications
                    .Where(n => n.UserId == user.Id && ids.Contains(n.Id))
                    .ToListAsync();

                if (toDelete.Any())
                {
                    _context.Notifications.RemoveRange(toDelete);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Seçilen bildirimler silindi.";
            return RedirectToAction(nameof(Notifications));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPrescriptionPdf(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientUserId == user.Id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Reçete bulunamadı veya bu reçeteye erişim yetkiniz yok.";
                return RedirectToAction(nameof(MedicalHistory));
            }

            if (appointment.PrescriptionCreatedAt == null || string.IsNullOrWhiteSpace(appointment.PrescriptionMedications))
            {
                TempData["ErrorMessage"] = "Bu randevu için henüz bir reçete oluşturulmamış.";
                return RedirectToAction(nameof(MedicalHistory));
            }

            var model = new HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel
            {
                AppointmentId = appointment.Id,
                PatientName = appointment.PatientName,
                PatientSurname = appointment.PatientSurname,
                DoctorName = $"{appointment.Doctor?.Title} {appointment.Doctor?.Name} {appointment.Doctor?.Surname}".Trim(),
                DepartmentName = appointment.Doctor?.Department?.Name ?? string.Empty,
                PrescriptionDate = appointment.PrescriptionCreatedAt.Value,
                Diagnosis = appointment.PrescriptionDiagnosis ?? "-",
                Medications = appointment.PrescriptionMedications,
                Notes = appointment.PrescriptionNotes
            };

            var pdf = SimplePdfGenerator.CreatePrescriptionPdf(model);
            return File(pdf, "application/pdf", $"recete-{appointment.Id}.pdf");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult CheckSymptoms([FromBody] SymptomRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Text))
                    return BadRequest("Lütfen şikayetinizi belirtin.");

                var lowerText = request.Text.ToLower(new System.Globalization.CultureInfo("tr-TR"));

                // 1. Selamlaşma
                var greetings = new[] { "selam", "merhaba", "naber", "nasılsın", "kimsin", "ismin ne" };
                if (greetings.Any(g => lowerText.Contains(g)))
                {
                    return Json(new { IsChat = true, Message = "Merhaba! Ben ENS. Bugün kendinizi nasıl hissediyorsunuz? Şikayetinizi kısaca yazar mısınız?" });
                }

                // 2. Akıllı Soru-Cevap Mantığı (Dinamik Teşhis)
                // Baş Ağrısı Akışı
                if ((lowerText.Contains("baş") || lowerText.Contains("bas")) && lowerText.Contains("ağrı"))
                {
                    if (!lowerText.Contains("mide") && !lowerText.Contains("bulantı") && !lowerText.Contains("ışık"))
                    {
                        return Json(new { IsChat = true, Message = "Baş ağrınızın yanında mide bulantısı veya ışığa karşı hassasiyet var mı? (Bu bilgiler migren olasılığını değerlendirmem için önemli)" });
                    }
                }

                // Karın Ağrısı Akışı
                if ((lowerText.Contains("karın") || lowerText.Contains("karin") || lowerText.Contains("mide")) && lowerText.Contains("ağrı"))
                {
                    if (!lowerText.Contains("şiddet") && !lowerText.Contains("sağ") && !lowerText.Contains("sol"))
                    {
                        return Json(new { IsChat = true, Message = "Anladım. Ağrınız karnınızın tam olarak neresinde? Sağ alt tarafta bir batma hissi veya şiddetli bir kramp var mı?" });
                    }
                }

                // Göğüs Ağrısı Akışı
                if (lowerText.Contains("göğüs") || lowerText.Contains("gogus") || lowerText.Contains("kalp"))
                {
                    if (!lowerText.Contains("nefes") && !lowerText.Contains("kol") && !lowerText.Contains("çarpıntı"))
                    {
                        return Json(new { IsChat = true, Message = "Göğüs ağrınızla birlikte nefes darlığı veya sol kolunuzda bir uyuşma hissediyor musunuz?" });
                    }
                }

                // 3. Semptom Analizi
                var symptomMap = new Dictionary<string, string[]>
                {
                    { "bas_agrisi", new[] { "baş", "bas", "agri", "ağrı", "şakak", "zonklama", "migren", "ışık", "hassasiyet" } },
                    { "bas_donmesi", new[] { "dönme", "donme", "denge", "sersemlik", "tansiyon" } },
                    { "ates", new[] { "ateş", "ates", "sıcaklık", "titreme", "soğuk algınlığı" } },
                    { "gogus_agrisi", new[] { "göğüs", "gogus", "kalp", "sıkışma", "nefes", "çarpıntı", "uyuşma" } },
                    { "karin_agrisi", new[] { "karın", "karin", "mide", "bulantı", "kusma", "kramp", "batma" } },
                    { "eklem_agrisi", new[] { "eklem", "diz", "omuz", "kemik", "romatizma" } },
                    { "bogaz_agrisi", new[] { "boğaz", "yutkunma", "öksürük", "faranjit" } },
                    { "dokuntu", new[] { "kaşıntı", "döküntü", "egzama", "alerji" } }
                };

                var detectedKeys = new List<string>();
                foreach (var mapping in symptomMap)
                {
                    if (mapping.Value.Any(keyword => lowerText.Contains(keyword)))
                        detectedKeys.Add(mapping.Key);
                }

                if (!detectedKeys.Any())
                {
                    return Json(new { IsChat = true, Message = "Üzgünüm, şikayetinizi tam anlayamadım. Biraz daha detay verebilir misiniz ya da 'Dahiliye' gibi genel bir bölüm önermemi ister misiniz?" });
                }

                var suggestions = _symptomCheckerService.Analyze(detectedKeys);
                return Json(new { IsChat = false, Suggestions = suggestions });
            }
            catch
            {
                return Json(new { IsChat = true, Message = "Küçük bir teknik aksaklık oldu, lütfen şikayetinizi tekrar yazar mısınız?" });
            }
        }

        private bool IsValidFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(extension)) return false;

            Span<byte> header = stackalloc byte[8];
            using var stream = file.OpenReadStream();
            var read = stream.Read(header);

            return extension.ToLowerInvariant() switch
            {
                ".pdf" => read >= 4 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
                ".png" => read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                _ => false
            };
        }

        public class SymptomRequest { public string Text { get; set; } }
    }
}
