using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.Services;
using HastaneRandevuSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Controllers
{
    [Authorize(Roles = "Hasta")]
    public class PatientController : Controller
    {
        private static readonly IReadOnlyDictionary<string, decimal> DepartmentPriceMap =
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Diş Sağlığı ve Hastalıkları"] = 1250m,
                ["Dahiliye (İç Hastalıkları)"] = 1100m,
                ["Kardiyoloji"] = 2200m,
                ["Nöroloji"] = 2100m,
                ["Ortopedi ve Travmatoloji"] = 1850m,
                ["Göz Hastalıkları"] = 1450m,
                ["Kulak Burun Boğaz"] = 1400m,
                ["Genel Cerrahi"] = 2300m,
                ["Dermatoloji"] = 1200m,
                ["Pediatri"] = 1150m,
                ["Psikiyatri"] = 1600m,
                ["Üroloji"] = 1750m,
                ["Fizik Tedavi ve Rehabilitasyon"] = 1350m,
                ["Kadın Hastalıkları ve Doğum"] = 1900m,
                ["Göğüs Hastalıkları"] = 1550m,
                ["Enfeksiyon Hastalıkları"] = 1300m,
                ["Beyin ve Sinir Cerrahisi"] = 2750m
            };

        private const decimal DefaultDepartmentFee = 1350m;
        private static readonly HashSet<string> AllowedMedicalHistoryExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpeg", ".jpg", ".png", ".doc", ".docx" };
        private const long MaxMedicalHistoryFileSizeInBytes = 8 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public PatientController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var appointments = await GetPatientAppointmentsQuery(user)
                .OrderByDescending(a => a.CreatedDate)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            var medicalHistoryCount = await _context.MedicalHistories.CountAsync(m => m.UserId == user.Id);
            var prescriptionCount = appointments.Count(a => !string.IsNullOrWhiteSpace(a.PrescriptionMedications));

            var now = DateTime.Now;
            var pendingAppointments = appointments
                .Where(a => a.AppointmentDate >= now && a.Status != AppointmentStatus.Iptal && a.Status != AppointmentStatus.Tamamlandi)
                .OrderByDescending(a => a.CreatedDate)
                .ThenByDescending(a => a.Id)
                .ToList();

            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .ToListAsync();

            var departmentFees = departments
                .Select(d => new DepartmentFeeItem
                {
                    DepartmentName = d.Name,
                    Fee = DepartmentPriceMap.TryGetValue(d.Name, out var fee) ? fee : DefaultDepartmentFee
                })
                .ToList();

            var model = new PatientDashboardViewModel
            {
                FullName = $"{user.Name} {user.Surname}".Trim(),
                Email = user.Email,
                Telefon = user.Telefon ?? user.PhoneNumber,
                TC = user.TC,
                DogumTarihi = user.DogumTarihi,
                Cinsiyet = user.Cinsiyet,
                PendingAppointmentsCount = pendingAppointments.Count,
                CompletedAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Tamamlandi),
                CancelledAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Iptal),
                PrescriptionCount = prescriptionCount,
                UnreadNotificationsCount = notifications.Count(n => !n.IsRead),
                MedicalHistoryCount = medicalHistoryCount,
                DepartmentFees = departmentFees,
                PendingAppointments = pendingAppointments.Take(5).ToList(),
                RecentAppointments = appointments.Take(5).ToList(),
                RecentNotifications = notifications.Take(5).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            return View(new PatientProfileViewModel
            {
                Name = user.Name ?? string.Empty,
                Surname = user.Surname ?? string.Empty,
                TC = user.TC ?? string.Empty,
                Telefon = user.Telefon ?? user.PhoneNumber ?? string.Empty,
                DogumTarihi = user.DogumTarihi,
                Cinsiyet = user.Cinsiyet ?? string.Empty,
                Adres = user.Adres ?? string.Empty,
                Email = user.Email ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(PatientProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            user.Name = model.Name;
            user.Surname = model.Surname;
            user.TC = model.TC;
            user.Telefon = model.Telefon;
            user.PhoneNumber = model.Telefon;
            user.DogumTarihi = model.DogumTarihi;
            user.Cinsiyet = model.Cinsiyet;
            user.Adres = model.Adres;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await CreateNotificationAsync(
                user.Id,
                "Profiliniz güncellendi",
                "Kişisel bilgileriniz başarıyla güncellendi.",
                "Profil",
                "/Patient/Profile");

            TempData["SuccessMessage"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

            await CreateNotificationAsync(
                user.Id,
                "Parolaniz degistirildi",
                "Hesap guvenliginiz icin parola degisikligi basariyla tamamlandi.",
                "Guvenlik",
                "/Patient/ChangePassword");

            TempData["SuccessMessage"] = "Parolaniz basariyla guncellendi.";
            return RedirectToAction(nameof(ChangePassword));
        }

        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReadAjax(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var unreadCount = await GetUnreadNotificationCountAsync(user.Id);
            return Json(new { success = true, unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            if (notifications.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsReadAjax()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            if (notifications.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, unreadCount = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(int[] ids)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (ids == null || ids.Length == 0)
            {
                TempData["ErrorMessage"] = "Silinecek bildirim seçilmedi.";
                return RedirectToAction(nameof(Notifications));
            }

            var toDelete = await _context.Notifications
                .Where(n => n.UserId == user.Id && ids.Contains(n.Id))
                .ToListAsync();

            if (toDelete.Count == 0)
            {
                TempData["ErrorMessage"] = "Silinecek bildirim bulunamadı.";
                return RedirectToAction(nameof(Notifications));
            }

            _context.Notifications.RemoveRange(toDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{toDelete.Count} bildirim silindi.";
            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var all = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .ToListAsync();

            if (all.Count == 0)
            {
                TempData["InfoMessage"] = "Silinecek bildirim yok.";
                return RedirectToAction(nameof(Notifications));
            }

            _context.Notifications.RemoveRange(all);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tüm bildirimler silindi.";
            return RedirectToAction(nameof(Notifications));
        }

        [HttpGet]
        public async Task<IActionResult> UnreadNotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { count = 0 });
            }

            var count = await GetUnreadNotificationCountAsync(user.Id);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> MedicalHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            return View(await BuildMedicalHistoryViewModelAsync(user.Id));
        }

        [HttpGet]
        public IActionResult Prescriptions()
        {
            return Redirect("/Patient/MedicalHistory#receteler");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicalHistory(PatientMedicalHistoryViewModel model, IFormFile? attachmentFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (model.VisitDate > DateTime.Today.AddDays(1))
            {
                ModelState.AddModelError(nameof(model.VisitDate), "Muayene tarihi gelecekte olamaz.");
            }

            string? relativeAttachmentPath = null;
            string? originalAttachmentName = null;
            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                if (attachmentFile.Length > MaxMedicalHistoryFileSizeInBytes)
                {
                    ModelState.AddModelError(string.Empty, "Dosya boyutu en fazla 8 MB olabilir.");
                }

                var extension = Path.GetExtension(attachmentFile.FileName);
                if (!AllowedMedicalHistoryExtensions.Contains(extension))
                {
                    ModelState.AddModelError(string.Empty, "Sadece PDF, JPG, PNG, DOC ve DOCX dosyalari yuklenebilir.");
                }

                if (ModelState.IsValid)
                {
                    var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "medical-history");
                    if (!Directory.Exists(uploadsRoot))
                    {
                        Directory.CreateDirectory(uploadsRoot);
                    }

                    var generatedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                    var fullPath = Path.Combine(uploadsRoot, generatedName);
                    await using var stream = new FileStream(fullPath, FileMode.Create);
                    await attachmentFile.CopyToAsync(stream);

                    relativeAttachmentPath = $"/uploads/medical-history/{generatedName}";
                    originalAttachmentName = attachmentFile.FileName;
                }
            }

            if (!ModelState.IsValid)
            {
                var history = await BuildMedicalHistoryViewModelAsync(user.Id);
                model.Records = history.Records;
                model.Prescriptions = history.Prescriptions;
                return View("MedicalHistory", model);
            }

            _context.MedicalHistories.Add(new MedicalHistory
            {
                UserId = user.Id,
                Title = model.Title.Trim(),
                Diagnosis = string.IsNullOrWhiteSpace(model.Diagnosis) ? null : model.Diagnosis.Trim(),
                Medications = string.IsNullOrWhiteSpace(model.Medications) ? null : model.Medications.Trim(),
                AllergyInfo = string.IsNullOrWhiteSpace(model.AllergyInfo) ? null : model.AllergyInfo.Trim(),
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                VisitDate = model.VisitDate.Date,
                AttachmentName = originalAttachmentName,
                AttachmentPath = relativeAttachmentPath,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await CreateNotificationAsync(
                user.Id,
                "Tibbi gecmis kaydi eklendi",
                $"{model.VisitDate:dd.MM.yyyy} tarihli yeni tibbi gecmis kaydiniz olusturuldu.",
                "TibbiGecmis",
                "/Patient/MedicalHistory");

            TempData["SuccessMessage"] = "Tibbi gecmis kaydi eklendi.";
            return RedirectToAction(nameof(MedicalHistory));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedicalHistory(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var record = await _context.MedicalHistories
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (record == null)
            {
                TempData["ErrorMessage"] = "Silinecek tibbi gecmis kaydi bulunamadi.";
                return RedirectToAction(nameof(MedicalHistory));
            }

            if (!string.IsNullOrWhiteSpace(record.AttachmentPath))
            {
                var physicalPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    record.AttachmentPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }

            _context.MedicalHistories.Remove(record);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tibbi gecmis kaydi silindi.";
            return RedirectToAction(nameof(MedicalHistory));
        }

        private IQueryable<Appointment> GetPatientAppointmentsQuery(AppUser user)
        {
            return _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a =>
                    a.PatientUserId == user.Id ||
                    (a.PatientUserId == null && a.PatientName == user.Name && a.PatientSurname == user.Surname));
        }

        private async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        private async Task<PatientMedicalHistoryViewModel> BuildMedicalHistoryViewModelAsync(string userId)
        {
            var records = await _context.MedicalHistories
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.VisitDate)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync();
            var prescriptions = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.PatientUserId == userId && !string.IsNullOrWhiteSpace(a.PrescriptionMedications))
                .OrderByDescending(a => a.PrescriptionCreatedAt ?? a.AppointmentDate)
                .ThenByDescending(a => a.Id)
                .Select(a => new PatientPrescriptionItemViewModel
                {
                    AppointmentId = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    PrescriptionDate = a.PrescriptionCreatedAt ?? a.AppointmentDate,
                    DoctorName = ((a.Doctor!.Title ?? string.Empty) + " " + a.Doctor.Name + " " + a.Doctor.Surname).Trim(),
                    DepartmentName = a.Doctor!.Department != null ? a.Doctor.Department.Name : string.Empty,
                    Diagnosis = a.PrescriptionDiagnosis ?? "-",
                    Medications = a.PrescriptionMedications!,
                    Notes = a.PrescriptionNotes
                })
                .ToListAsync();

            return new PatientMedicalHistoryViewModel
            {
                Records = records,
                Prescriptions = prescriptions
            };
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
        }
    }
}

