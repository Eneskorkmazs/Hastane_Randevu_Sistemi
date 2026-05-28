using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.Services;
using HastaneRandevuSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace HastaneRandevuSistemi.Controllers
{
    [Authorize(Roles = "Doktor")]
    public class DoctorToolsController : Controller
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
                Summary = "Monday, Wednesday and Friday, 09:00 - 12:00"
            },
            new DoctorAvailabilityPlan
            {
                WorkingDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday],
                WorkingHours = [13, 14, 15, 16],
                Summary = "Tuesday and Thursday, 13:00 - 16:00"
            },
            new DoctorAvailabilityPlan
            {
                WorkingDays = [DayOfWeek.Monday, DayOfWeek.Thursday],
                WorkingHours = [10, 11, 12, 13],
                Summary = "Monday and Thursday, 10:00 - 13:00"
            },
            new DoctorAvailabilityPlan
            {
                WorkingDays = [DayOfWeek.Tuesday, DayOfWeek.Friday],
                WorkingHours = [9, 10, 14, 15],
                Summary = "Tuesday and Friday, 09:00 - 10:00 and 14:00 - 15:00"
            }
        ];

        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMemoryCache _cache;

        public DoctorToolsController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
            {
                return NotFound();
            }

            var recentAppointments = await _context.Appointments
                .Include(a => a.PatientUser)
                .Include(a => a.Doctor)
                .Where(a => a.DoctorId == doctor.Id)
                .OrderByDescending(a => a.CreatedDate)
                .ThenByDescending(a => a.Id)
                .Take(10)
                .ToListAsync();

            var myPrescriptions = await _context.Appointments
                .Where(a => a.DoctorId == doctor.Id && a.PrescriptionCreatedAt != null)
                .OrderByDescending(a => a.PrescriptionCreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.DoctorName = $"{doctor.Title} {doctor.Name} {doctor.Surname}".Trim();
            ViewBag.DoctorId = doctor.Id;
            ViewBag.DepartmentName = doctor.Department?.Name ?? string.Empty;
            ViewBag.RecentAppointments = recentAppointments;
            ViewBag.MyPrescriptions = myPrescriptions;

            return View();
        }

        public async Task<IActionResult> Schedule(int monthOffset = 0)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
            {
                return NotFound();
            }

            var availabilityPlan = await GetDoctorAvailabilityPlanAsync(doctor.Id);
            if (availabilityPlan == null)
            {
                return NotFound();
            }

            var targetMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(monthOffset);
            var nextMonth = targetMonth.AddMonths(1);
            var holidayMap = BuildHolidayMap(targetMonth.Year, nextMonth.Year);

            var monthAppointments = await _context.Appointments
                .Include(a => a.PatientUser)
                .Where(a => a.DoctorId == doctor.Id
                    && a.AppointmentDate >= targetMonth
                    && a.AppointmentDate < nextMonth)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var todayAppointments = monthAppointments
                .Where(a => a.AppointmentDate.Date == DateTime.Today)
                .Select(MapAppointment)
                .ToList();

            var upcomingRaw = await _context.Appointments
                .Include(a => a.PatientUser)
                .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate > DateTime.Now)
                .OrderBy(a => a.AppointmentDate)
                .Take(8)
                .ToListAsync();
            var upcomingAppointments = upcomingRaw.Select(MapAppointment).ToList();

            var model = new DoctorScheduleViewModel
            {
                DoctorName = $"{doctor.Title} {doctor.Name} {doctor.Surname}".Trim(),
                DepartmentName = doctor.Department?.Name ?? string.Empty,
                Title = doctor.Title,
                CurrentMonthOffset = monthOffset,
                MonthTitle = targetMonth.ToString("MMMM yyyy", new CultureInfo("tr-TR")),
                AppointmentCountThisMonth = monthAppointments.Count,
                TodayAppointmentCount = todayAppointments.Count,
                UpcomingAppointmentCount = upcomingAppointments.Count,
                DailyCapacityText = $"{availabilityPlan.WorkingDays.Length} days and {availabilityPlan.WorkingHours.Length} hours",
                WorkingDays = availabilityPlan.WorkingDays.Select(GetTurkishDayName).ToList(),
                WorkingHours = availabilityPlan.WorkingHours.Select(hour => $"{hour:00}:00").ToList(),
                Summary = availabilityPlan.Summary,
                NextAvailableSlots = await GetNextAvailableSlotsAsync(doctor.Id, availabilityPlan),
                Weeks = BuildCalendarWeeks(targetMonth, monthAppointments, availabilityPlan, holidayMap),
                DayDetails = BuildDayDetails(targetMonth, monthAppointments, availabilityPlan, holidayMap),
                TodayAppointments = todayAppointments,
                UpcomingAppointments = upcomingAppointments,
                PrescriptionWeeks = BuildPrescriptionWeeks(upcomingRaw)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DayDetails(string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return RedirectToAction(nameof(Schedule));
            }

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var availabilityPlan = await GetDoctorAvailabilityPlanAsync(doctor.Id);
            var map = BuildHolidayMap(parsedDate.Year);
            var isHoliday = map.TryGetValue(DateOnly.FromDateTime(parsedDate), out var holidayLabel);

            var appointments = await _context.Appointments
                .Include(a => a.PatientUser)
                .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate.Date == parsedDate.Date)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.SelectedDate = parsedDate;
            ViewBag.IsHoliday = isHoliday;
            ViewBag.HolidayLabel = holidayLabel;
            ViewBag.IsWorkingDay = availabilityPlan!.WorkingDays.Contains(parsedDate.DayOfWeek);

            return View(appointments.Select(MapAppointment));
        }

        [HttpGet]
        public async Task<IActionResult> Prescription(int appointmentId)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.DoctorId == doctor.Id);

            if (appointment == null) return NotFound();

            if (appointment.AppointmentDate > DateTime.Now.AddMinutes(5))
            {
                TempData["ErrorMessage"] = "Tamamlanmamış randevu. Randevu saati henüz gelmediği için reçete oluşturulamaz.";
                return RedirectToAction(nameof(Index));
            }

            return View(new HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel
            {
                AppointmentId = appointment.Id,
                PatientName = appointment.PatientName,
                PatientSurname = appointment.PatientSurname,
                DoctorName = $"{doctor.Title} {doctor.Name} {doctor.Surname}".Trim(),
                DepartmentName = appointment.Doctor?.Department?.Name ?? string.Empty,
                PrescriptionDate = appointment.PrescriptionCreatedAt ?? DateTime.Now,
                Diagnosis = appointment.PrescriptionDiagnosis ?? string.Empty,
                Medications = appointment.PrescriptionMedications ?? string.Empty,
                Notes = appointment.PrescriptionNotes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prescription(HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel model)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == model.AppointmentId && a.DoctorId == doctor.Id);

            if (appointment == null) return NotFound();

            if (appointment.AppointmentDate > DateTime.Now.AddMinutes(5))
            {
                TempData["ErrorMessage"] = "Tamamlanmamış randevu. Randevu süreci henüz başlamadığı için reçete kaydedilemez.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                model.DoctorName = $"{doctor.Title} {doctor.Name} {doctor.Surname}".Trim();
                model.DepartmentName = appointment.Doctor?.Department?.Name ?? string.Empty;
                model.PatientName = appointment.PatientName;
                model.PatientSurname = appointment.PatientSurname;
                return View(model);
            }

            model.DoctorName = $"{doctor.Title} {doctor.Name} {doctor.Surname}".Trim();
            model.DepartmentName = appointment.Doctor?.Department?.Name ?? string.Empty;
            model.PatientName = appointment.PatientName;
            model.PatientSurname = appointment.PatientSurname;
            model.PrescriptionDate = DateTime.Now;

            appointment.PrescriptionDiagnosis = model.Diagnosis.Trim();
            appointment.PrescriptionMedications = model.Medications.Trim();
            appointment.PrescriptionNotes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
            appointment.PrescriptionCreatedAt = model.PrescriptionDate;
            appointment.PrescriptionSentAt = null;
            appointment.PrescriptionSentByName = null;
            
            // Eğer randevu henüz tamamlanmamışsa otomatik tamamla
            if (appointment.Status != AppointmentStatus.Tamamlandi)
            {
                appointment.Status = AppointmentStatus.Tamamlandi;
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(appointment.PatientUserId))
                _cache.Remove($"patient:prescriptions:{appointment.PatientUserId}");

            TempData["SuccessMessage"] = "Reçete başarıyla kaydedildi ve sekreter onayına gönderildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPrescriptionQr(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return NotFound();

            var qrText = $"HRS Recete\nRandevu No: {appointment.Id}\nHasta: {appointment.PatientName} {appointment.PatientSurname}\nDoktor: {appointment.Doctor?.Title} {appointment.Doctor?.Name} {appointment.Doctor?.Surname}\nPoliklinik: {appointment.Doctor?.Department?.Name ?? "-"}\nTarih: {appointment.PrescriptionCreatedAt?.ToString("dd.MM.yyyy") ?? appointment.AppointmentDate.ToString("dd.MM.yyyy")}\nTani: {appointment.PrescriptionDiagnosis ?? "-"}";

            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(8);
            return File(qrCodeImage, "image/png");
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

            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null || appointment.DoctorId != doctor.Id)
            {
                return Forbid();
            }

            if (appointment.PrescriptionCreatedAt == null || string.IsNullOrWhiteSpace(appointment.PrescriptionMedications))
            {
                return BadRequest("PDF oluşturmak için önce reçete kaydı hazırlanmalıdır.");
            }

            var model = new PrescriptionDraftViewModel
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

            var pdf = SimplePdfGenerator.CreatePrescriptionPdf(model);
            return File(pdf, "application/pdf", $"recete-{appointment.Id}.pdf");
        }

        private async Task<Doctor?> GetCurrentDoctorAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            return await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.UserId == user.Id)
                ?? await _context.Doctors
                    .Include(d => d.Department)
                    .FirstOrDefaultAsync(d => d.Name == user.Name && d.Surname == user.Surname);
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

        private async Task<IReadOnlyList<string>> GetNextAvailableSlotsAsync(int doctorId, DoctorAvailabilityPlan availabilityPlan)
        {
            var now = DateTime.Now;
            var recommendations = new List<string>();

            for (var dayOffset = 0; dayOffset < 14 && recommendations.Count < 5; dayOffset++)
            {
                var currentDate = DateTime.Today.AddDays(dayOffset);
                if (!availabilityPlan.WorkingDays.Contains(currentDate.DayOfWeek))
                {
                    continue;
                }

                var takenSlots = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == currentDate.Date && a.Status != AppointmentStatus.Iptal)
                    .Select(a => a.AppointmentDate.Hour)
                    .ToListAsync();

                foreach (var hour in availabilityPlan.WorkingHours)
                {
                    var candidate = currentDate.AddHours(hour);
                    if (candidate <= now || takenSlots.Contains(hour))
                    {
                        continue;
                    }

                    recommendations.Add(candidate.ToString("dd.MM.yyyy HH:mm"));
                }
            }

            return recommendations;
        }

        private static IReadOnlyList<DoctorCalendarWeekViewModel> BuildCalendarWeeks(
            DateTime monthStart,
            IReadOnlyList<Appointment> monthAppointments,
            DoctorAvailabilityPlan availabilityPlan,
            IReadOnlyDictionary<DateOnly, string> holidayMap)
        {
            var calendarStart = monthStart.AddDays(-(((int)monthStart.DayOfWeek + 6) % 7));
            var cells = new List<DoctorCalendarDayViewModel>(42);

            for (var index = 0; index < 42; index++)
            {
                var date = calendarStart.AddDays(index);
                var dailyAppointments = monthAppointments
                    .Where(a => a.AppointmentDate.Date == date.Date)
                    .OrderBy(a => a.AppointmentDate)
                    .Select(MapAppointment)
                    .ToList();

                cells.Add(new DoctorCalendarDayViewModel
                {
                    Date = date,
                    IsCurrentMonth = date.Month == monthStart.Month,
                    IsToday = date.Date == DateTime.Today,
                    IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                    IsSunday = date.DayOfWeek == DayOfWeek.Sunday,
                    IsWorkingDay = availabilityPlan.WorkingDays.Contains(date.DayOfWeek),
                    IsHoliday = holidayMap.TryGetValue(DateOnly.FromDateTime(date), out var holidayLabel),
                    HolidayLabel = holidayLabel ?? string.Empty,
                    AppointmentCount = dailyAppointments.Count,
                    Appointments = dailyAppointments.Take(3).ToList()
                });
            }

            return cells
                .Chunk(7)
                .Select(week => new DoctorCalendarWeekViewModel
                {
                    Days = week.ToList()
                })
                .ToList();
        }

        private static IReadOnlyList<DoctorCalendarDayDetailViewModel> BuildDayDetails(
            DateTime monthStart,
            IReadOnlyList<Appointment> monthAppointments,
            DoctorAvailabilityPlan availabilityPlan,
            IReadOnlyDictionary<DateOnly, string> holidayMap)
        {
            var calendarStart = monthStart.AddDays(-(((int)monthStart.DayOfWeek + 6) % 7));
            var details = new List<DoctorCalendarDayDetailViewModel>(42);

            for (var index = 0; index < 42; index++)
            {
                var date = calendarStart.AddDays(index);
                var dailyAppointments = monthAppointments
                    .Where(a => a.AppointmentDate.Date == date.Date)
                    .OrderBy(a => a.AppointmentDate)
                    .Select(MapAppointment)
                    .ToList();

                details.Add(new DoctorCalendarDayDetailViewModel
                {
                    DayKey = date.ToString("yyyy-MM-dd"),
                    Date = date,
                    DateLabel = date.ToString("dd MMMM yyyy, dddd", new CultureInfo("tr-TR")),
                    IsCurrentMonth = date.Month == monthStart.Month,
                    IsToday = date.Date == DateTime.Today,
                    IsSunday = date.DayOfWeek == DayOfWeek.Sunday,
                    IsWorkingDay = availabilityPlan.WorkingDays.Contains(date.DayOfWeek),
                    IsHoliday = holidayMap.TryGetValue(DateOnly.FromDateTime(date), out var holidayLabel),
                    HolidayLabel = holidayLabel ?? string.Empty,
                    AppointmentCount = dailyAppointments.Count,
                    Appointments = dailyAppointments
                });
            }

            return details;
        }

        private static DoctorCalendarAppointmentViewModel MapAppointment(Appointment appointment)
        {
            return new DoctorCalendarAppointmentViewModel
            {
                AppointmentId = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                PatientName = $"{appointment.PatientName} {appointment.PatientSurname}".Trim(),
                TimeLabel = appointment.AppointmentDate.ToString("HH:mm"),
                StatusText = GetStatusText(appointment.Status),
                StatusClass = GetStatusClass(appointment.Status)
            };
        }

        private static string GetStatusText(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Bekliyor => "Bekliyor",
                AppointmentStatus.Onaylandi => "Onayli",
                AppointmentStatus.Tamamlandi => "Tamamlandi",
                AppointmentStatus.Iptal => "Iptal",
                _ => "Bilinmiyor"
            };
        }

        private static string GetStatusClass(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Bekliyor => "bg-warning text-dark",
                AppointmentStatus.Onaylandi => "bg-primary",
                AppointmentStatus.Tamamlandi => "bg-success",
                AppointmentStatus.Iptal => "bg-danger",
                _ => "bg-secondary"
            };
        }

        private static string GetTurkishDayName(DayOfWeek dayOfWeek)
        {
            return new CultureInfo("tr-TR").DateTimeFormat.GetDayName(dayOfWeek);
        }

        private static IReadOnlyDictionary<DateOnly, string> BuildHolidayMap(params int[] years)
        {
            var map = new Dictionary<DateOnly, string>();
            var uniqueYears = years.Distinct();

            foreach (var year in uniqueYears)
            {
                AddHoliday(map, new DateOnly(year, 1, 1), "Yılbaşı ğŸ„");
                AddHoliday(map, new DateOnly(year, 4, 23), "23 Nisan Ulusal Egemenlik ve Çocuk Bayramı");
                AddHoliday(map, new DateOnly(year, 5, 1), "1 Mayıs Emek ve Dayanışma Günü");
                AddHoliday(map, new DateOnly(year, 5, 19), "19 Mayıs Atatürk'ü Anma, Gençlik ve Spor Bayramı");
                AddHoliday(map, new DateOnly(year, 10, 29), "29 Ekim Cumhuriyet Bayramı");
                AddHoliday(map, new DateOnly(year, 7, 15), "15 Temmuz Demokrasi ve Milli Birlik Günü");
                AddHoliday(map, new DateOnly(year, 8, 30), "30 Ağustos Zafer Bayramı");
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

        private static IReadOnlyList<DoctorPrescriptionWeekViewModel> BuildPrescriptionWeeks(IReadOnlyList<Appointment> upcomingAppointments)
        {
            var culture = new CultureInfo("tr-TR");
            var startOfWeek = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
            var weeks = new List<DoctorPrescriptionWeekViewModel>(4);

            for (var weekIndex = 0; weekIndex < 4; weekIndex++)
            {
                var weekStart = startOfWeek.AddDays(weekIndex * 7);
                var weekEnd = weekStart.AddDays(7);
                var count = upcomingAppointments.Count(a => a.AppointmentDate >= weekStart && a.AppointmentDate < weekEnd);

                weeks.Add(new DoctorPrescriptionWeekViewModel
                {
                    WeekLabel = $"{weekStart.ToString("dd MMM", culture)} - {weekEnd.AddDays(-1).ToString("dd MMM", culture)}",
                    UpcomingCount = count
                });
            }

            return weeks;
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

        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return View("~/Views/Patient/Notifications.cshtml", notifications);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadNotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { count = 0 });

            var count = await _context.Notifications.CountAsync(n => (n.UserId == user.Id) && !n.IsRead);
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReadAjax(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && (n.UserId == user.Id));
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var unreadCount = await _context.Notifications.CountAsync(n => (n.UserId == user.Id) && !n.IsRead);
            return Json(new { success = true, unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsReadAjax()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var unread = await _context.Notifications.Where(n => (n.UserId == user.Id) && !n.IsRead).ToListAsync();
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

            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && (n.UserId == user.Id));
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
                    .Where(n => (n.UserId == user.Id) && ids.Contains(n.Id))
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
        [AllowAnonymous]
        public async Task<IActionResult> SeedPastAppointments()
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Name == "Mehmet" && d.Surname == "Korkmaz");
            if (doctor == null) return Content("Doktor Mehmet Korkmaz bulunamadı.");

            var patient = await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.Contains("hasta")) 
                          ?? await _context.Users.FirstOrDefaultAsync();
            if (patient == null) return Content("Hasta kullanıcısı bulunamadı.");

            for (int i = 1; i <= 5; i++)
            {
                var app = new Appointment
                {
                    DoctorId = doctor.Id,
                    PatientUserId = patient.Id,
                    PatientName = patient.Name ?? "Test",
                    PatientSurname = patient.Surname ?? "Hasta",
                    AppointmentDate = DateTime.Now.AddDays(-i).Date.AddHours(9 + i),
                    Status = i % 2 == 0 ? AppointmentStatus.Onaylandi : AppointmentStatus.Bekliyor,
                    CreatedDate = DateTime.Now.AddDays(-10)
                };
                _context.Appointments.Add(app);
            }
            await _context.SaveChangesAsync();
            return Content("Başarılı! Mehmet Korkmaz için 5 adet geçmiş tarihli (tamamlanmamış) randevu oluşturuldu. Artık doktor panelinden reçete yazmayı deneyip engelini test edebilirsiniz.");
        }
    }
}

