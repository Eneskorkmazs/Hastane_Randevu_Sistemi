using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace HastaneRandevuSistemi.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private sealed class DoctorAvailabilityPlan
        {
            public required DayOfWeek[] WorkingDays { get; init; }
            public required int[] WorkingHours { get; init; }
            public required string Summary { get; init; }
        }

        private static readonly DoctorAvailabilityPlan[] AvailabilityTemplates =
        [
            new DoctorAvailabilityPlan
            {
                WorkingDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                WorkingHours = [9, 10, 11, 12],
                Summary = "Pazartesi, Çarşamba ve Cuma günleri 09:00 - 12:00 arasında"
            },
            new DoctorAvailabilityPlan
            {
                WorkingDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday],
                WorkingHours = [13, 14, 15, 16],
                Summary = "Salı ve Perşembe günleri 13:00 - 16:00 arasında"
            },
            new DoctorAvailabilityPlan
            {
                WorkingDays = [DayOfWeek.Monday, DayOfWeek.Thursday],
                WorkingHours = [10, 11, 12, 13],
                Summary = "Pazartesi ve Perşembe günleri 10:00 - 13:00 arasında"
            },
            new DoctorAvailabilityPlan
            {
                WorkingDays = [DayOfWeek.Tuesday, DayOfWeek.Friday],
                WorkingHours = [9, 10, 14, 15],
                Summary = "Salı ve Cuma günleri 09:00 - 10:00 ile 14:00 - 15:00 arasında"
            }
        ];

        private sealed class DepartmentLookupItem
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
        }

        private sealed class DoctorLookupItem
        {
            public int id { get; init; }
            public string name { get; init; } = string.Empty;
        }

        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly IMemoryCache _cache;

        public AppointmentController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            EmailService emailService,
            SmsService smsService,
            IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _smsService = smsService;
            _cache = cache;
        }

        private async Task<(string? UserId, string DisplayName)> GetCurrentActorAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var displayName = user != null
                ? $"{user.Name} {user.Surname}".Trim()
                : (User.Identity?.Name ?? "Sistem");

            return (user?.Id, displayName);
        }

        public async Task<IActionResult> Index(
            AppointmentStatus? status = null,
            int? doctorId = null,
            int? departmentId = null,
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? sortBy = null,
            bool onlyUpcoming = false)
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var normalizedFrom = fromDate?.Date;
            var normalizedTo = toDate?.Date;
            if (normalizedFrom.HasValue && normalizedTo.HasValue && normalizedFrom.Value > normalizedTo.Value)
            {
                var swap = normalizedFrom;
                normalizedFrom = normalizedTo;
                normalizedTo = swap;
            }

            var appointmentsQuery = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .AsQueryable();

            if (User.IsInRole("Doktor"))
            {
                var doctorIdByUser = await GetCurrentDoctorIdAsync();
                appointmentsQuery = doctorIdByUser.HasValue
                    ? appointmentsQuery.Where(a => a.DoctorId == doctorIdByUser.Value)
                    : appointmentsQuery.Where(a => a.Id == -1);
            }
            else if (User.IsInRole("Admin"))
            {
                appointmentsQuery = appointmentsQuery.Where(a =>
                    a.AdminAccessGranted ||
                    a.AdminAccessRequested ||
                    a.Status == AppointmentStatus.Bekliyor ||
                    a.Status == AppointmentStatus.Onaylandi);
            }
            else if (User.IsInRole("Hasta"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    appointmentsQuery = appointmentsQuery.Where(a =>
                        a.PatientUserId == user.Id ||
                        (a.PatientUserId == null && a.PatientName == user.Name && a.PatientSurname == user.Surname));
                }
            }

            if (User.IsInRole("Admin") && doctorId.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == doctorId);
            }

            if (departmentId.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Doctor != null && a.Doctor.DepartmentId == departmentId.Value);
            }

            if (status.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Status == status.Value);
            }

            if (normalizedFrom.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate >= normalizedFrom.Value);
            }

            if (normalizedTo.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate < normalizedTo.Value.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();

                appointmentsQuery = appointmentsQuery.Where(a =>
                    ((a.PatientName + " " + a.PatientSurname).ToLower().Contains(normalizedSearch)) ||
                    ((a.Doctor != null ? (a.Doctor.Name + " " + a.Doctor.Surname) : "").ToLower().Contains(normalizedSearch)) ||
                    ((a.Doctor != null && a.Doctor.Department != null ? a.Doctor.Department.Name : "").ToLower().Contains(normalizedSearch)));
            }

            if (onlyUpcoming)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate >= DateTime.Now);
            }

            ViewData["SelectedStatus"] = status.HasValue ? ((int)status.Value).ToString() : null;
            ViewData["SearchTerm"] = search;
            ViewData["DoctorId"] = doctorId;
            ViewData["DepartmentId"] = departmentId;
            ViewData["FromDate"] = normalizedFrom?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = normalizedTo?.ToString("yyyy-MM-dd");
            ViewData["SortBy"] = sortBy;
            ViewData["OnlyUpcoming"] = onlyUpcoming;

            if (User.IsInRole("Doktor"))
            {
                var doctorIdByUser = await GetCurrentDoctorIdAsync();
                ViewData["AccessRequestBadgeCount"] = doctorIdByUser.HasValue
                    ? await _context.Appointments.CountAsync(a => a.DoctorId == doctorIdByUser.Value && a.AdminAccessRequested && !a.AdminAccessGranted)
                    : 0;
            }
            else if (User.IsInRole("Admin"))
            {
                ViewData["AccessRequestBadgeCount"] = await _context.Appointments.CountAsync(a => a.AdminAccessRequested && !a.AdminAccessGranted);
            }

            ViewData["StatusOptions"] = Enum.GetValues(typeof(AppointmentStatus))
                .Cast<AppointmentStatus>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToString(),
                    Selected = status.HasValue && status.Value == s
                })
                .ToList();

            ViewData["DepartmentOptions"] = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name,
                    Selected = departmentId.HasValue && departmentId.Value == d.Id
                })
                .ToListAsync();

            if (User.IsInRole("Admin"))
            {
                ViewData["DoctorOptions"] = await _context.Doctors
                    .OrderBy(d => d.Name)
                    .ThenBy(d => d.Surname)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = (d.Title + " " + d.Name + " " + d.Surname).Trim(),
                        Selected = doctorId.HasValue && doctorId.Value == d.Id
                    })
                    .ToListAsync();
            }

            appointmentsQuery = sortBy switch
            {
                "date_asc" => appointmentsQuery.OrderBy(a => a.AppointmentDate),
                "status" => appointmentsQuery.OrderBy(a => a.Status).ThenByDescending(a => a.CreatedDate).ThenByDescending(a => a.Id),
                "doctor" => appointmentsQuery.OrderBy(a => a.Doctor!.Name).ThenBy(a => a.Doctor!.Surname).ThenByDescending(a => a.CreatedDate).ThenByDescending(a => a.Id),
                _ => appointmentsQuery.OrderByDescending(a => a.CreatedDate).ThenByDescending(a => a.Id)
            };

            return View(await appointmentsQuery.ToListAsync());
        }

        [Authorize(Roles = "Admin,Hasta")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.PatientName = user.Name;
                ViewBag.PatientSurname = user.Surname;
                ViewBag.PatientPhone = user.Telefon ?? user.PhoneNumber;
            }

            await LoadDepartmentSelectListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Hasta")]
        public async Task<IActionResult> Create([Bind("Id,AppointmentDate,PatientName,PatientSurname,PatientPhone,DoctorId")] Appointment appointment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && User.IsInRole("Hasta"))
            {
                appointment.PatientName = currentUser.Name ?? appointment.PatientName;
                appointment.PatientSurname = currentUser.Surname ?? appointment.PatientSurname;
                appointment.PatientPhone = string.IsNullOrWhiteSpace(appointment.PatientPhone)
                    ? currentUser.Telefon ?? currentUser.PhoneNumber
                    : appointment.PatientPhone;
                appointment.PatientUserId = currentUser.Id;
            }

            if (appointment.AppointmentDate <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(appointment.AppointmentDate), "Randevu tarihi ileri bir zaman olmalıdı.");
            }

            if (appointment.AppointmentDate.Minute != 0 || appointment.AppointmentDate.Hour < 9 || appointment.AppointmentDate.Hour > 16)
            {
                ModelState.AddModelError(nameof(appointment.AppointmentDate), "Randevular 09:00 - 16:00 arasında saat başlarında oluşturulabilir.");
            }

            var createHolidayMap = BuildHolidayMap(appointment.AppointmentDate.Year);
            if (createHolidayMap.TryGetValue(DateOnly.FromDateTime(appointment.AppointmentDate), out var createHolidayLabel))
            {
                ModelState.AddModelError(nameof(appointment.AppointmentDate), $"Secilen tarih resmi tatildir ({createHolidayLabel}). Bu gun randevu verilemez.");
            }

            var availabilityPlan = await GetDoctorAvailabilityPlanAsync(appointment.DoctorId);
            if (availabilityPlan == null)
            {
                ModelState.AddModelError(nameof(appointment.DoctorId), "Seçilen doktor için çalışma planı bulunamadı.");
            }
            else
            {
                if (!availabilityPlan.WorkingDays.Contains(appointment.AppointmentDate.DayOfWeek))
                {
                    ModelState.AddModelError(nameof(appointment.AppointmentDate), $"Seçilen doktor yalnızca {availabilityPlan.Summary} hizmet vermektedir.");
                }
                else if (!availabilityPlan.WorkingHours.Contains(appointment.AppointmentDate.Hour))
                {
                    ModelState.AddModelError(nameof(appointment.AppointmentDate), $"Seçilen doktorun müsait saatleri {string.Join(", ", availabilityPlan.WorkingHours.Select(hour => $"{hour:00}:00"))} olarak tanımlıdır.");
                }
            }

            var isSlotBusy = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == appointment.DoctorId &&
                a.AppointmentDate == appointment.AppointmentDate &&
                a.Status != AppointmentStatus.Iptal);

            if (isSlotBusy)
            {
                ModelState.AddModelError(string.Empty, "Seçilen doktora bu saatte zaten randevu alınmış.");
            }

            appointment.Status = AppointmentStatus.Bekliyor;
            appointment.CreatedDate = DateTime.Now;

            if (!ModelState.IsValid)
            {
                await LoadDepartmentSelectListAsync();
                return View(appointment);
            }

            _context.Add(appointment);
            await _context.SaveChangesAsync();

            var doctorForMessaging = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.Id == appointment.DoctorId);

            var doctorForMessageName = doctorForMessaging == null
                ? "doktorunuz"
                : $"{doctorForMessaging.Title} {doctorForMessaging.Name} {doctorForMessaging.Surname}".Trim();
            var departmentForMessageName = doctorForMessaging?.Department?.Name ?? "ilgili bölüm";

            await _smsService.SendAppointmentSmsAsync(
                appointment.PatientPhone ?? currentUser?.Telefon ?? currentUser?.PhoneNumber,
                $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli {departmentForMessageName} / {doctorForMessageName} randevunuz olusturuldu. Saglikli gunler.");

            var patientUserForMessaging = currentUser;
            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId) &&
                (patientUserForMessaging == null || patientUserForMessaging.Id != appointment.PatientUserId))
            {
                patientUserForMessaging = await _userManager.FindByIdAsync(appointment.PatientUserId);
            }

            if (!string.IsNullOrWhiteSpace(patientUserForMessaging?.Email))
            {
                var bookingMailBody = $@"
<div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
    <h2 style='color: #2c3e50;'>Hastane Randevu Sistemi</h2>
    <h3 style='color: #34495e;'>Randevunuz Olusturuldu</h3>
    <p style='font-size: 16px; color: #555;'>{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli {departmentForMessageName} / {doctorForMessageName} randevunuz olusturulmustur.</p>
    <p style='font-size: 14px; color: #777;'>Randevu No: {appointment.Id}</p>
    <hr style='border: 0; border-top: 1px solid #ddd; margin: 20px 0;'/>
    <p style='font-size: 12px; color: #aaa;'>Saglikli gunler dileriz.</p>
</div>";

                await _emailService.SendEmailAsync(
                    patientUserForMessaging.Email,
                    "Randevunuz olusturuldu",
                    bookingMailBody);
            }

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevunuz oluşturuldu",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} için {departmentForMessageName} / {doctorForMessageName} randevunuz alındı.",
                    "Randevu",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Randevunuz başarıyla oluşturuldu.";
            if (User.IsInRole("Hasta"))
            {
                return RedirectToAction("Dashboard", "Patient");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doktor")]
        public async Task<IActionResult> SendReminder(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Hatirlatma gonderilecek randevu bulunamadi.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Doktor") && !await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
            {
                return Forbid();
            }

            if (User.IsInRole("Admin") && !appointment.AdminAccessGranted)
            {
                TempData["ErrorMessage"] = "Bu randevu icin once doktorun admin erisim izni vermesi gerekir.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.Status is AppointmentStatus.Iptal or AppointmentStatus.Tamamlandi)
            {
                TempData["InfoMessage"] = "Sadece aktif randevular icin hatirlatma gonderilebilir.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.AppointmentDate <= DateTime.Now)
            {
                TempData["InfoMessage"] = "Gecmis randevular icin hatirlatma gonderilemez.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                TempData["ErrorMessage"] = "Bu randevuya bagli bir hasta kullanicisi bulunamadigi icin hatirlatma gonderilemedi.";
                return RedirectToAction(nameof(Index));
            }

            var doctorName = appointment.Doctor == null
                ? "doktorunuz"
                : $"{appointment.Doctor.Title} {appointment.Doctor.Name} {appointment.Doctor.Surname}".Trim();
            var departmentName = appointment.Doctor?.Department?.Name ?? "ilgili bolum";
            var reminderText = appointment.AppointmentDate - DateTime.Now <= TimeSpan.FromHours(24)
                ? "Randevu saatiniz yaklasiyor."
                : "Randevu tarihinizi unutmayiniz.";

            await CreateNotificationAsync(
                appointment.PatientUserId,
                "Randevu hatirlatmasi",
                $"{reminderText} {appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihindeki {departmentName} / {doctorName} randevunuz icin bilgilendirme mesajidir.",
                "Hatirlatma",
                "/Appointment/Index");

            TempData["SuccessMessage"] = "Hatirlatma bildirimi ve e-postasi gonderildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doktor")]
        public async Task<IActionResult> Approve(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Doktor") && !await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
            {
                return Forbid();
            }

            if (User.IsInRole("Admin") && !appointment.AdminAccessGranted)
            {
                TempData["ErrorMessage"] = "Bu randevu icin once doktorun admin erisim izni vermesi gerekir.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.Status == AppointmentStatus.Tamamlandi)
            {
                TempData["InfoMessage"] = "Tamamlanmış randevular tekrar onaylanamaz.";
                return RedirectToAction(nameof(Index));
            }

            var actor = await GetCurrentActorAsync();

            appointment.Status = AppointmentStatus.Onaylandi;
            appointment.ApprovedByUserId = actor.UserId;
            appointment.ApprovedByName = actor.DisplayName;
            appointment.ApprovedDate = DateTime.Now;
            appointment.IsCollected = true;
            appointment.CollectedDate ??= DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevunuz onaylandı",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz onaylandı ve odemeniz alindi.",
                    "Durum",
                    "/Appointment/Index");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doktor,Hasta")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var isAdmin = User.IsInRole("Admin");
            var isDoctor = User.IsInRole("Doktor");
            var isPatient = User.IsInRole("Hasta");

            if (appointment.AppointmentDate <= DateTime.Now)
            {
                TempData["InfoMessage"] = "Geçmiş randevular iptal edilemez. Yalnızca tamamlandı olarak kapatılabilir.";
                return RedirectToAction(nameof(Index));
            }

            if (isDoctor && !await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
            {
                return Forbid();
            }

            if (isAdmin && !appointment.AdminAccessGranted)
            {
                TempData["ErrorMessage"] = "Bu randevu icin once doktorun admin erisim izni vermesi gerekir.";
                return RedirectToAction(nameof(Index));
            }

            if (isPatient)
            {
                var patientUser = await _userManager.GetUserAsync(User);
                var isOwner = patientUser != null && (
                    appointment.PatientUserId == patientUser.Id ||
                    (appointment.PatientUserId == null && appointment.PatientName == patientUser.Name && appointment.PatientSurname == patientUser.Surname));

                if (!isOwner || appointment.Status is AppointmentStatus.Iptal or AppointmentStatus.Tamamlandi)
                {
                    TempData["ErrorMessage"] = "Bu randevu iptal edilemez.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (!isAdmin && !isDoctor && !isPatient)
            {
                return Forbid();
            }
            var wasCollectedBeforeCancel = appointment.IsCollected;
            var actor = await GetCurrentActorAsync();

            appointment.Status = AppointmentStatus.Iptal;
            appointment.CancelledByUserId = actor.UserId;
            appointment.CancelledByName = actor.DisplayName;
            appointment.CancelledDate = DateTime.Now;

            // Iptal edilen randevuda daha once odeme alinmissa kaydi geri al (iade).
            if (wasCollectedBeforeCancel)
            {
                appointment.IsCollected = false;
                appointment.CollectedDate = null;

                if (isPatient)
                {
                    appointment.CancelledByName = $"{actor.DisplayName} (Admin tarafindan odemesi geri iade edildi)";
                }
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                var cancelMessage = $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz iptal edildi.";
                if (wasCollectedBeforeCancel)
                {
                    cancelMessage += " Admin tarafindan odeme iadeniz yapildi.";
                }

                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevu durumu guncellendi",
                    cancelMessage,
                    "Durum",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = wasCollectedBeforeCancel
                ? "Randevu iptal edildi ve odeme iadesi yapildi."
                : "Randevu iptal edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doktor")]
        public async Task<IActionResult> Complete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Doktor") && !await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
            {
                return Forbid();
            }

            if (User.IsInRole("Admin") && !appointment.AdminAccessGranted)
            {
                TempData["ErrorMessage"] = "Bu randevu icin once doktorun admin erisim izni vermesi gerekir.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["InfoMessage"] = "Gelecek tarihli randevular tamamlandı olarak işaretlenemez.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.Status == AppointmentStatus.Iptal)
            {
                TempData["InfoMessage"] = "İptal edilmiş randevular tamamlandı olarak işaretlenemez.";
                return RedirectToAction(nameof(Index));
            }

            appointment.Status = AppointmentStatus.Tamamlandi;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevunuz tamamlandı",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli muayeneniz tamamlandı.",
                    "Durum",
                    "/Appointment/Index");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkCollected(int id)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Odeme islenecek randevu bulunamadi.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.Status == AppointmentStatus.Iptal)
            {
                TempData["ErrorMessage"] = "Iptal edilen randevu icin odeme alinamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Randevu bitmeden odeme alindi olarak isaretlenemez.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.Status != AppointmentStatus.Tamamlandi)
            {
                appointment.Status = AppointmentStatus.Tamamlandi;
            }

            appointment.IsCollected = true;
            appointment.CollectedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Odemeniz alindi",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz icin odeme kaydi tamamlandi.",
                    "Odeme",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Odeme alindi olarak guncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelCollected(int id)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Odeme kaydi bulunamadi.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Randevu bitmeden odeme kaydi degistirilemez.";
                return RedirectToAction(nameof(Index));
            }

            appointment.IsCollected = false;
            appointment.CollectedDate = null;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Odeme kaydi guncellendi",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz icin odeme kaydi iptal edildi.",
                    "Odeme",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Odeme iptal edildi olarak guncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RequestDoctorAccess(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Erisim istenen randevu bulunamadi.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.AdminAccessGranted)
            {
                TempData["InfoMessage"] = "Bu randevu icin doktor izni zaten verilmis.";
                return RedirectToAction(nameof(Index));
            }

            if (appointment.AdminAccessRequested)
            {
                TempData["InfoMessage"] = "Bu randevu icin doktor onayi zaten bekleniyor.";
                return RedirectToAction(nameof(Index));
            }

            var actor = await GetCurrentActorAsync();
            appointment.AdminAccessRequested = true;
            appointment.AdminAccessRequestedDate = DateTime.Now;
            appointment.AdminAccessRequestedByUserId = actor.UserId;
            appointment.AdminAccessRequestedByName = actor.DisplayName;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.Doctor?.UserId))
            {
                await CreateNotificationAsync(
                    appointment.Doctor.UserId,
                    "Admin erisim talebi",
                    $"{actor.DisplayName}, {appointment.PatientName} {appointment.PatientSurname} randevusu icin izin istiyor.",
                    "Izin",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Doktora erisim talebi gonderildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doktor")]
        public async Task<IActionResult> GrantAdminAccess(int id)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Onaylanacak erisim talebi bulunamadi.";
                return RedirectToAction(nameof(Index));
            }

            if (!await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
            {
                return Forbid();
            }

            if (!appointment.AdminAccessRequested)
            {
                TempData["ErrorMessage"] = "Erisim izni verilmedi.";
                return RedirectToAction(nameof(DoctorAccessRequests));
            }

            var actor = await GetCurrentActorAsync();

            appointment.AdminAccessRequested = false;
            appointment.AdminAccessGranted = true;
            appointment.AdminAccessGrantedDate = DateTime.Now;
            appointment.AdminAccessGrantedByUserId = actor.UserId;
            appointment.AdminAccessGrantedByName = actor.DisplayName;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.AdminAccessRequestedByUserId))
            {
                await CreateNotificationAsync(
                    appointment.AdminAccessRequestedByUserId,
                    "Doktor erisim izni verdi",
                    $"{appointment.PatientName} {appointment.PatientSurname} randevusu icin admin erisimi onaylandi.",
                    "Izin",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Admin erisim izni verildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doktor")]
        public async Task<IActionResult> DenyAdminAccess(int id)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Reddedilecek erisim talebi bulunamadi.";
                return RedirectToAction(nameof(DoctorAccessRequests));
            }

            if (!await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
            {
                return Forbid();
            }

            var requestedByUserId = appointment.AdminAccessRequestedByUserId;
            appointment.AdminAccessRequested = false;
            appointment.AdminAccessRequestedDate = null;
            appointment.AdminAccessRequestedByUserId = null;
            appointment.AdminAccessRequestedByName = null;
            appointment.AdminAccessGranted = false;
            appointment.AdminAccessGrantedDate = null;
            appointment.AdminAccessGrantedByUserId = null;
            appointment.AdminAccessGrantedByName = null;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(requestedByUserId))
            {
                await CreateNotificationAsync(
                    requestedByUserId,
                    "Doktor erisim talebini reddetti",
                    $"{appointment.PatientName} {appointment.PatientSurname} randevusu icin admin erisim talebi reddedildi.",
                    "Izin",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Erisim izni verilmedi.";
            return RedirectToAction(nameof(DoctorAccessRequests));
        }

        [HttpGet]
        [Authorize(Roles = "Doktor")]
        public async Task<IActionResult> DoctorAccessRequests()
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue)
            {
                return NotFound();
            }

            var requests = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.DoctorId == doctorId.Value && (a.AdminAccessRequested || a.AdminAccessGranted))
                .OrderByDescending(a => a.AdminAccessRequestedDate ?? a.AdminAccessGrantedDate ?? a.CreatedDate)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "departmentId" })]
        public async Task<IActionResult> GetDoctorsByDepartment(int departmentId)
        {
            var cacheKey = $"lookup:doctors:department:{departmentId}";
            if (!_cache.TryGetValue(cacheKey, out IReadOnlyList<DoctorLookupItem>? doctors))
            {
                doctors = await _context.Doctors
                    .AsNoTracking()
                    .Where(d => d.DepartmentId == departmentId)
                    .OrderBy(d => d.Name)
                    .ThenBy(d => d.Surname)
                    .Select(d => new DoctorLookupItem
                    {
                        id = d.Id,
                        name = (d.Title + " " + d.Name + " " + d.Surname).Trim()
                    })
                    .ToListAsync();

                _cache.Set(cacheKey, doctors, TimeSpan.FromMinutes(10));
            }

            return Json(doctors);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctorAvailability(int doctorId)
        {
            var availabilityPlan = await GetDoctorAvailabilityPlanAsync(doctorId);
            if (availabilityPlan == null)
            {
                return NotFound();
            }

            return Json(new
            {
                workingDays = availabilityPlan.WorkingDays.Select(day => (int)day),
                workingDayNames = availabilityPlan.WorkingDays.Select(GetTurkishDayName),
                availableHours = availabilityPlan.WorkingHours.Select(hour => $"{hour:00}:00"),
                summary = availabilityPlan.Summary
            });
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "doctorId", "date" })]
        public async Task<IActionResult> GetTakenSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var selectedDate))
            {
                return BadRequest();
            }

            var availabilityPlan = await GetDoctorAvailabilityPlanAsync(doctorId);
            if (availabilityPlan == null)
            {
                return NotFound();
            }

            var holidayMap = BuildHolidayMap(selectedDate.Year);
            var isHoliday = holidayMap.TryGetValue(DateOnly.FromDateTime(selectedDate), out var holidayLabel);
            if (isHoliday)
            {
                return Json(new
                {
                    takenSlots = Array.Empty<string>(),
                    availableHours = availabilityPlan.WorkingHours.Select(hour => $"{hour:00}:00"),
                    isAvailableDay = false,
                    isHoliday = true,
                    holidayLabel = holidayLabel ?? string.Empty,
                    summary = availabilityPlan.Summary,
                    message = "Bugun resmi tatil."
                });
            }

            var isAvailableDay = availabilityPlan.WorkingDays.Contains(selectedDate.DayOfWeek);
            var taken = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == selectedDate.Date && a.Status != AppointmentStatus.Iptal)
                .Select(a => a.AppointmentDate.ToString("HH:mm"))
                .ToListAsync();

            return Json(new
            {
                takenSlots = taken,
                availableHours = availabilityPlan.WorkingHours.Select(hour => $"{hour:00}:00"),
                isAvailableDay,
                isHoliday = false,
                holidayLabel = string.Empty,
                summary = availabilityPlan.Summary,
                message = isAvailableDay
                    ? "Musait saatler listelendi."
                    : $"Bu doktor {availabilityPlan.Summary} hizmet vermektedir."
            });
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendedSlots(int doctorId, string? startDate)
        {
            var searchStart = DateTime.TryParse(startDate, out var parsedDate)
                ? parsedDate.Date
                : DateTime.Today;

            var availabilityPlan = await GetDoctorAvailabilityPlanAsync(doctorId);
            if (availabilityPlan == null)
            {
                return Json(Array.Empty<object>());
            }

            var recommendations = new List<object>();
            var holidayMap = BuildHolidayMap(searchStart.Year, searchStart.AddDays(21).Year);
            for (var dayOffset = 0; dayOffset < 21 && recommendations.Count < 5; dayOffset++)
            {
                var currentDate = searchStart.AddDays(dayOffset);
                if (!availabilityPlan.WorkingDays.Contains(currentDate.DayOfWeek))
                {
                    continue;
                }

                if (holidayMap.ContainsKey(DateOnly.FromDateTime(currentDate)))
                {
                    continue;
                }

                var takenSlots = (await _context.Appointments
                    .AsNoTracking()
                    .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == currentDate.Date && a.Status != AppointmentStatus.Iptal)
                    .Select(a => a.AppointmentDate.Hour)
                    .ToListAsync())
                    .ToHashSet();

                foreach (var hour in availabilityPlan.WorkingHours)
                {
                    var candidate = currentDate.AddHours(hour);
                    if (candidate <= DateTime.Now || takenSlots.Contains(hour))
                    {
                        continue;
                    }

                    recommendations.Add(new
                    {
                        value = candidate.ToString("yyyy-MM-ddTHH:mm"),
                        label = candidate.ToString("dd.MM.yyyy HH:mm")
                    });
                }
            }

            return Json(recommendations);
        }

        private async Task<bool> IsCurrentDoctorOwnerAsync(int doctorId)
        {
            var currentDoctorId = await GetCurrentDoctorIdAsync();
            return currentDoctorId.HasValue && currentDoctorId.Value == doctorId;
        }

        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            var doctorId = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.UserId == user.Id)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            if (doctorId.HasValue)
            {
                return doctorId;
            }

            return await _context.Doctors
                .AsNoTracking()
                .Where(d => d.Name == user.Name && d.Surname == user.Surname)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<DoctorAvailabilityPlan?> GetDoctorAvailabilityPlanAsync(int doctorId)
        {
            var doctor = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.Id == doctorId)
                .Select(d => new { d.Id, d.DepartmentId })
                .FirstOrDefaultAsync();

            if (doctor == null)
            {
                return null;
            }

            var departmentDoctorIds = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.DepartmentId == doctor.DepartmentId)
                .OrderBy(d => d.Id)
                .Select(d => d.Id)
                .ToListAsync();

            var doctorIndex = departmentDoctorIds.IndexOf(doctorId);
            if (doctorIndex < 0)
            {
                doctorIndex = 0;
            }

            return AvailabilityTemplates[doctorIndex % AvailabilityTemplates.Length];
        }

        private static string GetTurkishDayName(DayOfWeek dayOfWeek)
        {
            return new CultureInfo("tr-TR").DateTimeFormat.GetDayName(dayOfWeek);
        }

        private static IReadOnlyDictionary<DateOnly, string> BuildHolidayMap(params int[] years)
        {
            var map = new Dictionary<DateOnly, string>();
            foreach (var year in years.Distinct())
            {
                AddHoliday(map, new DateOnly(year, 1, 1), "Yilbasi");
                AddHoliday(map, new DateOnly(year, 4, 23), "23 Nisan Ulusal Egemenlik ve Cocuk Bayrami");
                AddHoliday(map, new DateOnly(year, 5, 1), "1 Mayis Emek ve Dayanisma Gunu");
                AddHoliday(map, new DateOnly(year, 5, 19), "19 Mayis Ataturk'u Anma, Genclik ve Spor Bayrami");
                AddHoliday(map, new DateOnly(year, 7, 15), "15 Temmuz Demokrasi ve Milli Birlik Gunu");
                AddHoliday(map, new DateOnly(year, 8, 30), "30 Agustos Zafer Bayrami");
                AddHoliday(map, new DateOnly(year, 10, 29), "29 Ekim Cumhuriyet Bayrami");
            }

            return map;
        }

        private static void AddHoliday(IDictionary<DateOnly, string> map, DateOnly date, string label)
        {
            if (!map.ContainsKey(date))
            {
                map[date] = label;
            }
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
    <p style='font-size: 12px; color: #aaa;'>Sağlıklı günler dileriz.</p>
</div>";
                    await _emailService.SendEmailAsync(user.Email, title, emailBody);
                }
            }
            catch
            {
                // Mail gonderimi basarisiz olursa akisi bolmemek icin sessizce gec
            }
        }

        private async Task LoadDepartmentSelectListAsync(int? selectedDepartmentId = null)
        {
            const string cacheKey = "lookup:departments";
            if (!_cache.TryGetValue(cacheKey, out IReadOnlyList<DepartmentLookupItem>? departments))
            {
                departments = await _context.Departments
                    .AsNoTracking()
                    .OrderBy(d => d.Name)
                    .Select(d => new DepartmentLookupItem { Id = d.Id, Name = d.Name })
                    .ToListAsync();

                _cache.Set(cacheKey, departments, TimeSpan.FromMinutes(20));
            }

            ViewData["DepartmentId"] = new SelectList(departments, nameof(DepartmentLookupItem.Id), nameof(DepartmentLookupItem.Name), selectedDepartmentId);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Doktor,Hasta")]
        public async Task<IActionResult> GetQrTicket(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (appointment == null) return NotFound();
            
            var deptName = appointment.Doctor?.Department?.Name ?? "Belirtilmemiş";
            var qrText = $"Randevu No: {appointment.Id}\nHasta: {appointment.PatientName} {appointment.PatientSurname}\nTarih: {appointment.AppointmentDate:dd.MM.yyyy HH:mm}\nPoliklinik: {deptName}\nDurum: {appointment.Status}";
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(10);
            return File(qrCodeImage, "image/png");
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Doktor,Hasta")]
        public async Task<IActionResult> Details(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Include(a => a.MedicalReports)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            var isHasta = User.IsInRole("Hasta");
            var isDoctor = User.IsInRole("Doktor");

            if (isHasta && appointment.PatientUserId != _userManager.GetUserId(User))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && (appointment.PatientName != user.Name || appointment.PatientSurname != user.Surname))
                {
                    return Forbid();
                }
            }

            if (isDoctor && !await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
            {
                return Forbid();
            }

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doktor")]
        public async Task<IActionResult> UploadReport(int appointmentId, IFormFile reportFile, string notes)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return NotFound();

            if (User.IsInRole("Doktor") && !await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
                return Forbid();

            var rootPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports");
            if (!System.IO.Directory.Exists(rootPath)) System.IO.Directory.CreateDirectory(rootPath);
            
            if (reportFile != null && reportFile.Length > 0)
            {
                var ext = System.IO.Path.GetExtension(reportFile.FileName).ToLower();
                var allowed = new[] { ".pdf", ".jpeg", ".jpg", ".png", ".doc", ".docx" };
                if (!allowed.Contains(ext))
                {
                    TempData["ErrorMessage"] = "Gecersiz dosya turu.";
                    return RedirectToAction("Details", new { id = appointmentId });
                }

                var fileName = Guid.NewGuid().ToString() + ext;
                var fullPath = System.IO.Path.Combine(rootPath, fileName);
                
                using (var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
                {
                    await reportFile.CopyToAsync(stream);
                }
                
                _context.MedicalReports.Add(new MedicalReport
                {
                    AppointmentId = appointmentId,
                    FileName = reportFile.FileName,
                    FilePath = "/uploads/reports/" + fileName,
                    Notes = notes,
                    UploadedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
                {
                    await CreateNotificationAsync(appointment.PatientUserId, "Yeni Dosya Eklendi", $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuza laboratuvar/test sonuclari eklendi.", "Dosya", $"/Appointment/Details/{appointment.Id}");
                }

                TempData["SuccessMessage"] = "Dosya basariyla eklendi.";
            }

            return RedirectToAction("Details", new { id = appointmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doktor")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var report = await _context.MedicalReports.Include(r => r.Appointment).FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();

            if (User.IsInRole("Doktor") && report.Appointment != null && !await IsCurrentDoctorOwnerAsync(report.Appointment!.DoctorId))
                return Forbid();

            var physicalPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", report.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
            
            _context.MedicalReports.Remove(report);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Dosya silindi.";
            return RedirectToAction("Details", new { id = report.AppointmentId });
        }
    }
}



