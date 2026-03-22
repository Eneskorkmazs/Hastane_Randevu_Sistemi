using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;

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

        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AppointmentController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                        Text = d.Name + " " + d.Surname,
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

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name");
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
                ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name");
                return View(appointment);
            }

            _context.Add(appointment);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                var doctor = await _context.Doctors
                    .Include(d => d.Department)
                    .FirstOrDefaultAsync(d => d.Id == appointment.DoctorId);

                var doctorName = doctor == null ? "doktorunuz" : $"{doctor.Title} {doctor.Name} {doctor.Surname}".Trim();
                var departmentName = doctor?.Department?.Name ?? "ilgili bölüm";

                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevunuz oluşturuldu",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} için {departmentName} / {doctorName} randevunuz alındı.",
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
        public async Task<IActionResult> Delete(int id)
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

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

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

            appointment.Status = AppointmentStatus.Onaylandi;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevunuz onaylandı",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz onaylandı.",
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

            // Geçmiş randevular iptal edilemez; gerekiyorsa tamamlanmışa çek.
            if (appointment.AppointmentDate <= DateTime.Now)
            {
                if (appointment.Status != AppointmentStatus.Tamamlandi && appointment.Status != AppointmentStatus.Iptal)
                {
                    appointment.Status = AppointmentStatus.Tamamlandi;
                    await _context.SaveChangesAsync();
                    TempData["InfoMessage"] = "Süresi geçen randevu iptal edilmedi, tamamlandı olarak işaretlendi.";
                }
                else
                {
                    TempData["InfoMessage"] = "Süresi geçen randevular iptal edilemez.";
                }

                return RedirectToAction(nameof(Index));
            }

            if (isDoctor && !await IsCurrentDoctorOwnerAsync(appointment.DoctorId))
            {
                return Forbid();
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

            appointment.Status = AppointmentStatus.Iptal;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
            {
                await CreateNotificationAsync(
                    appointment.PatientUserId,
                    "Randevu durumu güncellendi",
                    $"{appointment.AppointmentDate:dd.MM.yyyy HH:mm} tarihli randevunuz iptal edildi.",
                    "Durum",
                    "/Appointment/Index");
            }

            TempData["SuccessMessage"] = "Randevu iptal edildi.";
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

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["InfoMessage"] = "Gelecek tarihli randevular tamamlandı olarak işaretlenemez.";
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

        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetDoctorsByDepartment(int departmentId)
        {
            var doctors = _context.Doctors
                .Where(d => d.DepartmentId == departmentId)
                .Select(d => new { id = d.Id, name = d.Name + " " + d.Surname })
                .ToList();

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

            var isAvailableDay = availabilityPlan.WorkingDays.Contains(selectedDate.DayOfWeek);
            var taken = _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == selectedDate.Date && a.Status != AppointmentStatus.Iptal)
                .Select(a => a.AppointmentDate.ToString("HH:mm"))
                .ToList();

            return Json(new
            {
                takenSlots = taken,
                availableHours = availabilityPlan.WorkingHours.Select(hour => $"{hour:00}:00"),
                isAvailableDay,
                summary = availabilityPlan.Summary,
                message = isAvailableDay
                    ? "Müsait saatler listelendi."
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
            for (var dayOffset = 0; dayOffset < 21 && recommendations.Count < 5; dayOffset++)
            {
                var currentDate = searchStart.AddDays(dayOffset);
                if (!availabilityPlan.WorkingDays.Contains(currentDate.DayOfWeek))
                {
                    continue;
                }

                var takenSlots = _context.Appointments
                    .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == currentDate.Date && a.Status != AppointmentStatus.Iptal)
                    .Select(a => a.AppointmentDate.Hour)
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
                .Where(d => d.UserId == user.Id)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            if (doctorId.HasValue)
            {
                return doctorId;
            }

            return await _context.Doctors
                .Where(d => d.Name == user.Name && d.Surname == user.Surname)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<DoctorAvailabilityPlan?> GetDoctorAvailabilityPlanAsync(int doctorId)
        {
            var doctor = await _context.Doctors
                .Where(d => d.Id == doctorId)
                .Select(d => new { d.Id, d.DepartmentId })
                .FirstOrDefaultAsync();

            if (doctor == null)
            {
                return null;
            }

            var departmentDoctorIds = await _context.Doctors
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
