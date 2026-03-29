using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.Services;
using HastaneRandevuSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HastaneRandevuSistemi.Controllers
{
    public class HomeController : Controller
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

        private const decimal DefaultAppointmentFee = 1350m;

        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        // UserManager'ı ekledik ki doktorun kim olduğunu bulabilelim
        private readonly UserManager<AppUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // 1. ANA SAYFA (VİTRİN) - Herkes Görebilir
        public IActionResult Index()
        {
            return View();
        }

        // 2. ADMIN DASHBOARD - Sadece Admin Görebilir
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var today = DateTime.Today;
            var now = DateTime.Now;
            var registeredPatientCount = await _context.Appointments
                .Where(a => a.PatientUserId != null)
                .Select(a => a.PatientUserId!)
                .Distinct()
                .CountAsync();

            var guestPatientCount = await _context.Appointments
                .Where(a => a.PatientUserId == null)
                .Select(a => new { a.PatientName, a.PatientSurname, a.PatientPhone })
                .Distinct()
                .CountAsync();

            var model = new DashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                ActiveAppointments = await _context.Appointments.CountAsync(a => a.Status != AppointmentStatus.Iptal),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Bekliyor),
                ApprovedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Onaylandi),
                CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Tamamlandi),
                CancelledAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Iptal),
                ThisWeekAppointments = await GetWeekAppointmentCountAsync(today),
                TodaysAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == today),
                RemainingTodayAppointments = await _context.Appointments.CountAsync(a =>
                    a.AppointmentDate.Date == today &&
                    a.AppointmentDate >= now &&
                    a.Status != AppointmentStatus.Iptal),
                PendingPaymentAppointments = await _context.Appointments.CountAsync(a =>
                    a.AppointmentDate <= now &&
                    a.Status != AppointmentStatus.Iptal &&
                    !a.IsCollected),
                UniquePatientCount = registeredPatientCount + guestPatientCount,
                LatestNotifications = await _context.Notifications
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(7)
                    .ToListAsync(),
                DepartmentStats = await GetDepartmentStatsAsync(),
                WeeklyTrend = await GetWeeklyTrendAsync(today)
            };

            return View(model); // Views/Home/AdminDashboard.cshtml sayfasına gider
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReport(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, AppointmentStatus? status = null)
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var normalizedFrom = fromDate?.Date ?? DateTime.Today.AddDays(-30);
            var normalizedTo = toDate?.Date ?? DateTime.Today;
            if (normalizedFrom > normalizedTo)
            {
                (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
            }

            var reportQuery = _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .AsQueryable();

            reportQuery = reportQuery.Where(a => a.AppointmentDate >= normalizedFrom && a.AppointmentDate < normalizedTo.AddDays(1));

            if (departmentId.HasValue)
            {
                reportQuery = reportQuery.Where(a => a.Doctor != null && a.Doctor.DepartmentId == departmentId.Value);
            }

            if (status.HasValue)
            {
                reportQuery = reportQuery.Where(a => a.Status == status.Value);
            }

            var appointments = await reportQuery
                .OrderByDescending(a => a.CreatedDate)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            ViewData["DepartmentOptions"] = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name,
                    Selected = departmentId.HasValue && d.Id == departmentId.Value
                })
                .ToListAsync();

            ViewData["StatusOptions"] = Enum.GetValues(typeof(AppointmentStatus))
                .Cast<AppointmentStatus>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToString(),
                    Selected = status.HasValue && status.Value == s
                })
                .ToList();

            var departmentStats = appointments
                .GroupBy(a => a.Doctor?.Department?.Name ?? "Bilinmeyen")
                .Select(g => new DepartmentStatItem
                {
                    DepartmentName = g.Key,
                    AppointmentCount = g.Count(),
                    DoctorCount = g.Select(x => x.DoctorId).Distinct().Count()
                })
                .OrderByDescending(x => x.AppointmentCount)
                .ToList();

            var patientCount = appointments
                .Select(a => $"{a.PatientName}|{a.PatientSurname}|{a.PatientPhone}")
                .Distinct()
                .Count();

            return View(new AdminReportViewModel
            {
                FromDate = normalizedFrom,
                ToDate = normalizedTo,
                DepartmentId = departmentId,
                Status = status,
                TotalAppointments = appointments.Count,
                TotalPatients = patientCount,
                TotalDoctors = appointments.Select(a => a.DoctorId).Distinct().Count(),
                CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Iptal),
                CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Tamamlandi),
                DepartmentStats = departmentStats,
                RecentAppointments = appointments.Take(12).ToList()
            });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAccounting(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null)
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var normalizedFrom = fromDate?.Date ?? DateTime.Today.AddDays(-30);
            var normalizedTo = toDate?.Date ?? DateTime.Today;
            if (normalizedFrom > normalizedTo)
            {
                (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
            }

            var appointmentsQuery = _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.CreatedDate >= normalizedFrom && a.CreatedDate < normalizedTo.AddDays(1));

            if (departmentId.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Doctor != null && a.Doctor.DepartmentId == departmentId.Value);
            }

            var appointments = await appointmentsQuery
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            ViewData["DepartmentOptions"] = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name,
                    Selected = departmentId.HasValue && d.Id == departmentId.Value
                })
                .ToListAsync();

            decimal GetFee(Appointment appointment)
            {
                var departmentName = appointment.Doctor?.Department?.Name;
                return departmentName != null && DepartmentPriceMap.TryGetValue(departmentName, out var fee)
                    ? fee
                    : DefaultAppointmentFee;
            }

            var estimatedRevenue = appointments
                .Where(a => a.Status != AppointmentStatus.Iptal)
                .Sum(GetFee);

            var collectedRevenue = appointments
                .Where(a => a.IsCollected)
                .Sum(GetFee);

            var pendingRevenue = appointments
                .Where(a => !a.IsCollected && a.Status != AppointmentStatus.Iptal)
                .Sum(GetFee);

            var cancelledRevenue = appointments
                .Where(a => a.Status == AppointmentStatus.Iptal)
                .Sum(GetFee);

            var departmentBreakdown = appointments
                .GroupBy(a => a.Doctor?.Department?.Name ?? "Bilinmeyen")
                .Select(g => new AccountingDepartmentItem
                {
                    DepartmentName = g.Key,
                    AppointmentCount = g.Count(),
                    CompletedCount = g.Count(x => x.IsCollected),
                    EstimatedRevenue = g.Where(x => x.Status != AppointmentStatus.Iptal).Sum(GetFee),
                    CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetFee),
                    PendingRevenue = g.Where(x => !x.IsCollected && x.Status != AppointmentStatus.Iptal).Sum(GetFee)
                })
                .OrderByDescending(x => x.CollectedRevenue)
                .ThenByDescending(x => x.EstimatedRevenue)
                .ToList();

            var doctorBreakdown = appointments
                .GroupBy(a => new
                {
                    DoctorName = a.Doctor == null
                        ? "Bilinmeyen Doktor"
                        : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen"
                })
                .Select(g => new AccountingDoctorItem
                {
                    DoctorName = g.Key.DoctorName,
                    DepartmentName = g.Key.DepartmentName,
                    AppointmentCount = g.Count(),
                    CompletedCount = g.Count(x => x.IsCollected),
                    EstimatedRevenue = g.Where(x => x.Status != AppointmentStatus.Iptal).Sum(GetFee),
                    CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetFee)
                })
                .OrderByDescending(x => x.CollectedRevenue)
                .ThenByDescending(x => x.AppointmentCount)
                .Take(10)
                .ToList();

            AccountingSelectedDepartmentSummary? selectedDepartmentSummary = null;
            IReadOnlyList<AccountingMonthlyDepartmentStat> monthlyDepartmentStats = Array.Empty<AccountingMonthlyDepartmentStat>();

            if (departmentId.HasValue)
            {
                var selectedDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == departmentId.Value);

                if (selectedDepartment != null)
                {
                    var departmentAppointments = appointments
                        .Where(a => a.Doctor?.DepartmentId == departmentId.Value)
                        .ToList();

                    var topDoctorInSelectedDepartment = departmentAppointments
                        .GroupBy(a => a.Doctor == null
                            ? "Bilinmeyen Doktor"
                            : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim())
                        .Select(g => new
                        {
                            DoctorName = g.Key,
                            AppointmentCount = g.Count(),
                            Revenue = g.Where(x => x.IsCollected).Sum(GetFee)
                        })
                        .OrderByDescending(x => x.AppointmentCount)
                        .ThenByDescending(x => x.Revenue)
                        .FirstOrDefault();

                    selectedDepartmentSummary = new AccountingSelectedDepartmentSummary
                    {
                        DepartmentName = selectedDepartment.Name,
                        DoctorCount = await _context.Doctors.CountAsync(d => d.DepartmentId == departmentId.Value),
                        AppointmentCount = departmentAppointments.Count,
                        UniquePatientCount = departmentAppointments
                            .Select(a => $"{a.PatientName}|{a.PatientSurname}|{a.PatientPhone}")
                            .Distinct()
                            .Count(),
                        CollectedRevenue = departmentAppointments
                            .Where(a => a.IsCollected)
                            .Sum(GetFee),
                        PendingRevenue = departmentAppointments
                            .Where(a => !a.IsCollected && a.Status != AppointmentStatus.Iptal)
                            .Sum(GetFee),
                        TopDoctorName = topDoctorInSelectedDepartment?.DoctorName ?? "-",
                        TopDoctorAppointmentCount = topDoctorInSelectedDepartment?.AppointmentCount ?? 0,
                        TopDoctorRevenue = topDoctorInSelectedDepartment?.Revenue ?? 0
                    };

                    monthlyDepartmentStats = departmentAppointments
                        .GroupBy(a => new { a.CreatedDate.Year, a.CreatedDate.Month })
                        .OrderBy(g => g.Key.Year)
                        .ThenBy(g => g.Key.Month)
                        .Select(g => new AccountingMonthlyDepartmentStat
                        {
                            Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                            AppointmentCount = g.Count(),
                            UniquePatientCount = g
                                .Select(a => $"{a.PatientName}|{a.PatientSurname}|{a.PatientPhone}")
                                .Distinct()
                                .Count(),
                            CollectedRevenue = g.Where(a => a.IsCollected).Sum(GetFee),
                            PendingRevenue = g.Where(a => !a.IsCollected && a.Status != AppointmentStatus.Iptal).Sum(GetFee)
                        })
                        .ToList();
                }
            }

            var topDepartmentByAppointments = departmentBreakdown
                .OrderByDescending(x => x.AppointmentCount)
                .ThenByDescending(x => x.CollectedRevenue)
                .FirstOrDefault();

            var topDepartmentByRevenue = departmentBreakdown
                .OrderByDescending(x => x.CollectedRevenue)
                .ThenByDescending(x => x.AppointmentCount)
                .FirstOrDefault();

            var topDoctorByAppointments = doctorBreakdown
                .OrderByDescending(x => x.AppointmentCount)
                .ThenByDescending(x => x.CollectedRevenue)
                .FirstOrDefault();

            var topDoctorByRevenue = doctorBreakdown
                .OrderByDescending(x => x.CollectedRevenue)
                .ThenByDescending(x => x.AppointmentCount)
                .FirstOrDefault();

            var recentTransactions = appointments
                .Take(12)
                .Select(a => new AccountingLedgerItem
                {
                    AppointmentId = a.Id,
                    RecordedDate = a.CreatedDate,
                    AppointmentDate = a.AppointmentDate,
                    PatientName = $"{a.PatientName} {a.PatientSurname}".Trim(),
                    DoctorName = a.Doctor == null
                        ? "Bilinmeyen Doktor"
                        : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen",
                    StatusLabel = a.IsCollected ? "Odeme Yapildi" : a.Status switch
                    {
                        AppointmentStatus.Iptal => "Iptal / Kayip",
                        AppointmentStatus.Onaylandi => "Odeme Bekliyor",
                        _ => "Planlandi"
                    },
                    Amount = GetFee(a),
                    IsCollected = a.IsCollected,
                    IsAppointmentFinished = a.AppointmentDate <= DateTime.Now
                })
                .ToList();

            var billableAppointments = appointments.Count(a => a.Status != AppointmentStatus.Iptal);

            return View(new AdminAccountingViewModel
            {
                FromDate = normalizedFrom,
                ToDate = normalizedTo,
                DepartmentId = departmentId,
                TotalAppointments = appointments.Count,
                CompletedAppointments = appointments.Count(a => a.IsCollected),
                PendingAppointments = appointments.Count(a => !a.IsCollected && a.Status != AppointmentStatus.Iptal),
                CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Iptal),
                EstimatedRevenue = estimatedRevenue,
                CollectedRevenue = collectedRevenue,
                PendingRevenue = pendingRevenue,
                CancelledRevenue = cancelledRevenue,
                AverageTicket = billableAppointments == 0 ? 0 : estimatedRevenue / billableAppointments,
                TopDepartmentByAppointments = topDepartmentByAppointments?.DepartmentName ?? "-",
                TopDepartmentAppointmentCount = topDepartmentByAppointments?.AppointmentCount ?? 0,
                TopDoctorByAppointments = topDoctorByAppointments?.DoctorName ?? "-",
                TopDoctorAppointmentCount = topDoctorByAppointments?.AppointmentCount ?? 0,
                TopDepartmentByRevenue = topDepartmentByRevenue?.DepartmentName ?? "-",
                TopDepartmentRevenue = topDepartmentByRevenue?.CollectedRevenue ?? 0,
                TopDoctorByRevenue = topDoctorByRevenue?.DoctorName ?? "-",
                TopDoctorRevenue = topDoctorByRevenue?.CollectedRevenue ?? 0,
                SelectedDepartmentSummary = selectedDepartmentSummary,
                DepartmentBreakdown = departmentBreakdown,
                DoctorBreakdown = doctorBreakdown,
                RecentTransactions = recentTransactions,
                MonthlyDepartmentStats = monthlyDepartmentStats
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkCollected(int id, DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Odeme islenecek kayit bulunamadi.";
                return RedirectToAction(nameof(AdminAccounting), new { fromDate, toDate, departmentId });
            }

            if (appointment.Status == AppointmentStatus.Iptal)
            {
                TempData["ErrorMessage"] = "Iptal edilen randevu icin odeme islenemez.";
                return RedirectToAction(nameof(AdminAccounting), new { fromDate, toDate, departmentId });
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Randevu bitmeden odeme yapildi olarak isaretlenemez.";
                return RedirectToAction(nameof(AdminAccounting), new { fromDate, toDate, departmentId });
            }

            appointment.IsCollected = true;
            appointment.CollectedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Odeme yapildi olarak guncellendi.";
            return RedirectToAction(nameof(AdminAccounting), new { fromDate, toDate, departmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelCollected(int id, DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Odeme iptal edilecek kayit bulunamadi.";
                return RedirectToAction(nameof(AdminAccounting), new { fromDate, toDate, departmentId });
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Randevu bitmeden odeme kaydi degistirilemez.";
                return RedirectToAction(nameof(AdminAccounting), new { fromDate, toDate, departmentId });
            }

            appointment.IsCollected = false;
            appointment.CollectedDate = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Odeme iptal edildi olarak guncellendi.";
            return RedirectToAction(nameof(AdminAccounting), new { fromDate, toDate, departmentId });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateAnnouncement()
        {
            PopulateAnnouncementRoleOptions();
            return View(new AnnouncementCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAnnouncement(AnnouncementCreateViewModel model)
        {
            PopulateAnnouncementRoleOptions();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usersQuery = _context.Users.AsQueryable();
            if (!string.Equals(model.TargetRole, "All", StringComparison.OrdinalIgnoreCase))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(model.TargetRole);
                var userIds = usersInRole.Select(u => u.Id).ToHashSet();
                usersQuery = usersQuery.Where(u => userIds.Contains(u.Id));
            }

            var users = await usersQuery.ToListAsync();
            if (users.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Seçilen hedef için kullanıcı bulunamadı.");
                return View(model);
            }

            var createdDate = DateTime.Now;
            await _context.Notifications.AddRangeAsync(users.Select(user => new Notification
            {
                UserId = user.Id,
                Title = model.Title,
                Message = model.Message,
                Type = "Duyuru",
                Link = model.Link,
                CreatedDate = createdDate
            }));

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{users.Count} kullaniciya duyuru gonderildi.";
            return RedirectToAction(nameof(AdminDashboard));
        }

        // 3. DOKTOR DASHBOARD - Sadece Doktor Görebilir
        [Authorize(Roles = "Doktor")]
        public async Task<IActionResult> DoctorDashboard()
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var user = await _userManager.GetUserAsync(User);
            var now = DateTime.Now;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            int myAppointments = 0;
            int myPending = 0;
            int myToday = 0;
            int myApproved = 0;
            int myCompleted = 0;
            int thisWeek = 0;
            int pendingAccessRequestCount = 0;
            IReadOnlyList<Appointment> upcomingAppointments = Array.Empty<Appointment>();

            if (user != null)
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id)
                             ?? await _context.Doctors.FirstOrDefaultAsync(d => d.Name == user.Name && d.Surname == user.Surname);

                if (doctor != null)
                {
                    myAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == doctor.Id);

                    myPending = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.Status == AppointmentStatus.Bekliyor);

                    myToday = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.AppointmentDate.Date == DateTime.Today);

                    myApproved = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.Status == AppointmentStatus.Onaylandi);

                    myCompleted = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.Status == AppointmentStatus.Tamamlandi);

                    thisWeek = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.AppointmentDate >= weekStart && a.AppointmentDate < weekEnd);

                    pendingAccessRequestCount = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.AdminAccessRequested && !a.AdminAccessGranted);

                    upcomingAppointments = await _context.Appointments
                        .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate >= now && a.Status != AppointmentStatus.Iptal)
                        .Include(a => a.PatientUser)
                        .OrderBy(a => a.AppointmentDate)
                        .Take(6)
                        .ToListAsync();
                }
            }

            var model = new DashboardViewModel
            {
                TotalAppointments = myAppointments,
                PendingAppointments = myPending,
                ApprovedAppointments = myApproved,
                CompletedAppointments = myCompleted,
                CancelledAppointments = 0,
                TodaysAppointments = myToday,
                ThisWeekAppointments = thisWeek,
                PendingAccessRequestCount = pendingAccessRequestCount,
                UpcomingAppointments = upcomingAppointments,
                TotalDoctors = 0,
                TotalDepartments = 0
            };

            return View(model); // Views/Home/DoctorDashboard.cshtml sayfasına gider
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<int> GetWeekAppointmentCountAsync(DateTime date)
        {
            var weekStart = date.Date.AddDays(-(int)date.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            return await _context.Appointments
                .CountAsync(a => a.AppointmentDate >= weekStart && a.AppointmentDate < weekEnd);
        }

        private async Task<IReadOnlyList<DepartmentStatItem>> GetDepartmentStatsAsync()
        {
            return await _context.Departments
                .Select(d => new DepartmentStatItem
                {
                    DepartmentName = d.Name,
                    DoctorCount = _context.Doctors.Count(doc => doc.DepartmentId == d.Id),
                    AppointmentCount = _context.Appointments.Count(a => a.Doctor != null && a.Doctor.DepartmentId == d.Id)
                })
                .OrderByDescending(x => x.AppointmentCount)
                .Take(6)
                .ToListAsync();
        }

        private async Task<IReadOnlyList<TrendPointItem>> GetWeeklyTrendAsync(DateTime date)
        {
            var weekStart = date.Date.AddDays(-(int)date.DayOfWeek);
            var result = new List<TrendPointItem>();

            for (var i = 0; i < 7; i++)
            {
                var dayStart = weekStart.AddDays(i);
                var dayEnd = dayStart.AddDays(1);
                result.Add(new TrendPointItem
                {
                    Label = dayStart.ToString("ddd"),
                    TotalCount = await _context.Appointments.CountAsync(a => a.AppointmentDate >= dayStart && a.AppointmentDate < dayEnd)
                });
            }

            return result;
        }

        private void PopulateAnnouncementRoleOptions()
        {
            ViewData["AnnouncementRoles"] = new List<SelectListItem>
            {
                new() { Value = "All", Text = "Tüm kullanıcılar" },
                new() { Value = "Hasta", Text = "Hastalar" },
                new() { Value = "Doktor", Text = "Doktorlar" },
                new() { Value = "Admin", Text = "Adminler" }
            };
        }
    }
}

