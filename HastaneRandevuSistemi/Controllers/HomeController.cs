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
using System.Globalization;

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
        private readonly UserManager<AppUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // 1. ANA SAYFA (VİTRİN) - Herkes Görebilir
        [ResponseCache(Duration = 180, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
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

            var allRevenueAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.Status != AppointmentStatus.Iptal)
                .ToListAsync();

            decimal GetFee(Appointment appointment)
            {
                var departmentName = appointment.Doctor?.Department?.Name;
                return departmentName != null && DepartmentPriceMap.TryGetValue(departmentName, out var fee)
                    ? fee
                    : DefaultAppointmentFee;
            }

            var totalRevenue = allRevenueAppointments.Where(a => a.IsCollected).Sum(GetFee);
            var pendingRevenue = allRevenueAppointments.Where(a => !a.IsCollected && a.AppointmentDate <= now).Sum(GetFee);

            var pendingPayments = allRevenueAppointments
                .Where(a => !a.IsCollected && a.AppointmentDate <= now)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(7)
                .Select(a => new AccountingLedgerItem
                {
                    AppointmentId = a.Id,
                    RecordedDate = a.CreatedDate,
                    AppointmentDate = a.AppointmentDate,
                    PatientName = $"{a.PatientName} {a.PatientSurname}".Trim(),
                    DoctorName = a.Doctor == null ? "Bilinmeyen Doktor" : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen",
                    StatusLabel = "Ödeme Bekliyor",
                    Amount = GetFee(a),
                    IsCollected = a.IsCollected,
                    IsAppointmentFinished = true
                }).ToList();

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
                WeeklyTrend = await GetWeeklyTrendAsync(today),
                TotalRevenue = totalRevenue,
                PendingRevenue = pendingRevenue,
                PendingPayments = pendingPayments
            };

            return View(model);
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
        public async Task<IActionResult> AdminAccounting(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, int? doctorId = null)
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

            if (doctorId.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Doctor != null && a.Doctor.Id == doctorId.Value);
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

            ViewData["DoctorOptions"] = await _context.Doctors
                .Include(d => d.Department)
                .Where(d => !departmentId.HasValue || d.DepartmentId == departmentId.Value)
                .OrderBy(d => d.Department != null ? d.Department.Name : string.Empty)
                .ThenBy(d => d.Name)
                .ThenBy(d => d.Surname)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Title} {d.Name} {d.Surname}".Trim() + (d.Department != null ? $" - {d.Department.Name}" : string.Empty),
                    Selected = doctorId.HasValue && d.Id == doctorId.Value
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
                .GroupBy(a => new
                {
                    DepartmentId = a.Doctor != null ? (int?)a.Doctor.DepartmentId : null,
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen"
                })
                .Select(g => new AccountingDepartmentItem
                {
                    DepartmentId = g.Key.DepartmentId,
                    DepartmentName = g.Key.DepartmentName,
                    DoctorCount = g.Where(x => x.Doctor != null).Select(x => x.Doctor!.Id).Distinct().Count(),
                    AppointmentCount = g.Count(),
                    UniquePatientCount = g
                        .Select(x => x.PatientUserId ?? $"{x.PatientName}|{x.PatientSurname}|{x.PatientPhone}")
                        .Distinct()
                        .Count(),
                    CompletedCount = g.Count(x => x.IsCollected),
                    CancelledCount = g.Count(x => x.Status == AppointmentStatus.Iptal),
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
                    DoctorId = a.Doctor != null ? (int?)a.Doctor.Id : null,
                    DepartmentId = a.Doctor != null ? (int?)a.Doctor.DepartmentId : null,
                    DoctorName = a.Doctor == null
                        ? "Bilinmeyen Doktor"
                        : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen"
                })
                .Select(g => new AccountingDoctorItem
                {
                    DoctorId = g.Key.DoctorId,
                    DepartmentId = g.Key.DepartmentId,
                    DoctorName = g.Key.DoctorName,
                    DepartmentName = g.Key.DepartmentName,
                    AppointmentCount = g.Count(),
                    UniquePatientCount = g
                        .Select(x => x.PatientUserId ?? $"{x.PatientName}|{x.PatientSurname}|{x.PatientPhone}")
                        .Distinct()
                        .Count(),
                    CompletedCount = g.Count(x => x.IsCollected),
                    CancelledCount = g.Count(x => x.Status == AppointmentStatus.Iptal),
                    EstimatedRevenue = g.Where(x => x.Status != AppointmentStatus.Iptal).Sum(GetFee),
                    CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetFee),
                    PendingRevenue = g.Where(x => !x.IsCollected && x.Status != AppointmentStatus.Iptal).Sum(GetFee),
                    CancelledRevenue = g.Where(x => x.Status == AppointmentStatus.Iptal).Sum(GetFee)
                })
                .OrderByDescending(x => x.CollectedRevenue)
                .ThenByDescending(x => x.AppointmentCount)
                .ToList();

            foreach (var item in departmentBreakdown)
            {
                item.RevenueSharePercent = collectedRevenue <= 0
                    ? 0
                    : Math.Round((item.CollectedRevenue / collectedRevenue) * 100m, 1);
            }

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

            var holidayMap = BuildHolidayMap(appointments.Select(a => a.AppointmentDate.Year).ToArray());

            var recentTransactions = appointments
                .Take(12)
                .Select(a =>
                {
                    var isHoliday = holidayMap.TryGetValue(DateOnly.FromDateTime(a.AppointmentDate), out var holidayLabel);

                    return new AccountingLedgerItem
                    {
                        AppointmentId = a.Id,
                        RecordedDate = a.CreatedDate,
                        AppointmentDate = a.AppointmentDate,
                        PatientName = $"{a.PatientName} {a.PatientSurname}".Trim(),
                        DoctorName = a.Doctor == null
                            ? "Bilinmeyen Doktor"
                            : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                        DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen",
                        StatusLabel = a.IsCollected ? "Ödeme Yapıldı" : isHoliday
                            ? $"Resmi Tatil ({holidayLabel})"
                            : a.Status switch
                            {
                                AppointmentStatus.Iptal when !string.IsNullOrWhiteSpace(a.PatientUserId)
                                    && a.CancelledByUserId == a.PatientUserId
                                    && (a.CancelledByName ?? string.Empty).Contains("Admin tarafindan odemesi geri iade edildi", StringComparison.OrdinalIgnoreCase)
                                        => "Admin tarafindan odemesi geri iade edildi / Iptal edildi",
                                AppointmentStatus.Iptal when !string.IsNullOrWhiteSpace(a.PatientUserId)
                                    && a.CancelledByUserId == a.PatientUserId => "Odeme iadesi yapildi / Iptal edildi",
                                AppointmentStatus.Iptal => "İptal / Kayıp",
                                AppointmentStatus.Onaylandi => "Ödeme Bekliyor",
                                _ => "Planlandı"
                            },
                        Amount = GetFee(a),
                        IsCollected = a.IsCollected,
                        IsAppointmentFinished = a.AppointmentDate <= DateTime.Now,
                        IsHoliday = isHoliday,
                        HolidayLabel = holidayLabel ?? string.Empty
                    };
                })
                .ToList();

            var pendingCollectionQueue = appointments
                .Where(a => !a.IsCollected && a.Status != AppointmentStatus.Iptal && a.AppointmentDate <= DateTime.Now)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a =>
                {
                    var isHoliday = holidayMap.TryGetValue(DateOnly.FromDateTime(a.AppointmentDate), out var holidayLabel);

                    return new AccountingLedgerItem
                    {
                        AppointmentId = a.Id,
                        RecordedDate = a.CreatedDate,
                        AppointmentDate = a.AppointmentDate,
                        PatientName = $"{a.PatientName} {a.PatientSurname}".Trim(),
                        DoctorName = a.Doctor == null
                            ? "Bilinmeyen Doktor"
                            : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                        DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen",
                        StatusLabel = isHoliday ? $"Resmi Tatil ({holidayLabel})" : "Odeme Bekliyor",
                        Amount = GetFee(a),
                        IsCollected = false,
                        IsAppointmentFinished = true,
                        IsHoliday = isHoliday,
                        HolidayLabel = holidayLabel ?? string.Empty
                    };
                })
                .ToList();

            var billableAppointments = appointments.Count(a => a.Status != AppointmentStatus.Iptal);

            var totalDays = (normalizedTo - normalizedFrom).TotalDays + 1;
            var useDailyTrend = totalDays <= 62;

            var revenueTrend = useDailyTrend
                ? appointments
                    .GroupBy(a => a.AppointmentDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new AccountingTrendPoint
                    {
                        Label = g.Key.ToString("dd MMM", new CultureInfo("tr-TR")),
                        AppointmentCount = g.Count(),
                        CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetFee),
                        PendingRevenue = g.Where(x => !x.IsCollected && x.Status != AppointmentStatus.Iptal).Sum(GetFee)
                    })
                    .ToList()
                : appointments
                    .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new AccountingTrendPoint
                    {
                        Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", new CultureInfo("tr-TR")),
                        AppointmentCount = g.Count(),
                        CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetFee),
                        PendingRevenue = g.Where(x => !x.IsCollected && x.Status != AppointmentStatus.Iptal).Sum(GetFee)
                    })
                    .ToList();

            return View(new AdminAccountingViewModel
            {
                FromDate = normalizedFrom,
                ToDate = normalizedTo,
                DepartmentId = departmentId,
                DoctorId = doctorId,
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
                MonthlyDepartmentStats = monthlyDepartmentStats,
                PendingCollectionQueue = pendingCollectionQueue,
                PendingCollectionQueueTotal = pendingCollectionQueue.Where(x => !x.IsHoliday).Sum(x => x.Amount),
                PendingCollectionQueueCount = pendingCollectionQueue.Count(x => !x.IsHoliday),
                RevenueTrend = revenueTrend
            });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDoctorInsights(int? doctorId = null, string? period = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var normalizedPeriod = string.IsNullOrWhiteSpace(period)
                ? "monthly"
                : period.Trim().ToLowerInvariant();

            if (normalizedPeriod is not ("weekly" or "monthly" or "custom"))
            {
                normalizedPeriod = "monthly";
            }

            var today = DateTime.Today;
            DateTime normalizedStart;
            DateTime normalizedEnd;

            if (normalizedPeriod == "weekly")
            {
                normalizedStart = StartOfWeek(today);
                normalizedEnd = normalizedStart.AddDays(6);
            }
            else if (normalizedPeriod == "custom")
            {
                normalizedStart = (startDate ?? today.AddDays(-30)).Date;
                normalizedEnd = (endDate ?? today).Date;
            }
            else
            {
                normalizedStart = new DateTime(today.Year, today.Month, 1);
                normalizedEnd = normalizedStart.AddMonths(1).AddDays(-1);
            }

            if (normalizedStart > normalizedEnd)
            {
                (normalizedStart, normalizedEnd) = (normalizedEnd, normalizedStart);
            }

            var doctors = await _context.Doctors
                .Include(d => d.Department)
                .OrderBy(d => d.Name)
                .ThenBy(d => d.Surname)
                .ToListAsync();

            if (doctorId.HasValue && doctors.All(d => d.Id != doctorId.Value))
            {
                doctorId = null;
            }

            var doctorOptions = new List<SelectListItem>
            {
                new() { Value = string.Empty, Text = "Tum Doktorlar", Selected = !doctorId.HasValue }
            };

            doctorOptions.AddRange(doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.Title} {d.Name} {d.Surname} ({d.Department?.Name ?? "Bilinmeyen"})".Trim(),
                Selected = doctorId.HasValue && d.Id == doctorId.Value
            }));

            var rangeAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.AppointmentDate >= normalizedStart && a.AppointmentDate < normalizedEnd.AddDays(1))
                .ToListAsync();

            var filteredAppointments = doctorId.HasValue
                ? rangeAppointments.Where(a => a.DoctorId == doctorId.Value).ToList()
                : rangeAppointments;

            decimal GetFee(Appointment appointment)
            {
                var departmentName = appointment.Doctor?.Department?.Name;
                return appointment.Price ?? (departmentName != null && DepartmentPriceMap.TryGetValue(departmentName, out var fee)
                    ? fee
                    : DefaultAppointmentFee);
            }

            var selectedDoctor = doctorId.HasValue
                ? doctors.FirstOrDefault(d => d.Id == doctorId.Value)
                : null;

            var totalAppointments = filteredAppointments.Count();
            var completedAppointments = filteredAppointments.Count(a => a.Status == AppointmentStatus.Tamamlandi);
            var approvedAppointments = filteredAppointments.Count(a => a.Status == AppointmentStatus.Onaylandi);
            var pendingAppointments = filteredAppointments.Count(a => a.Status == AppointmentStatus.Bekliyor);
            var cancelledAppointments = filteredAppointments.Count(a => a.Status == AppointmentStatus.Iptal);
            var collectedAppointments = filteredAppointments.Count(a => a.IsCollected);
            var uniquePatientCount = filteredAppointments
                .Select(a => a.PatientUserId ?? $"{a.PatientName}|{a.PatientSurname}|{a.PatientPhone}")
                .Distinct()
                .Count();

            var estimatedRevenue = filteredAppointments
                .Where(a => a.Status != AppointmentStatus.Iptal)
                .Sum(GetFee);

            var collectedRevenue = filteredAppointments
                .Where(a => a.IsCollected)
                .Sum(GetFee);

            var useDailyTrend = (normalizedEnd - normalizedStart).TotalDays <= 14;
            var trend = useDailyTrend
                ? filteredAppointments
                    .GroupBy(a => a.AppointmentDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DoctorInsightTrendItem
                    {
                        Label = g.Key.ToString("dd MMM", CultureInfo.GetCultureInfo("tr-TR")),
                        AppointmentCount = g.Count(),
                        CompletedCount = g.Count(a => a.Status == AppointmentStatus.Tamamlandi)
                    })
                    .ToList()
                : filteredAppointments
                    .GroupBy(a => StartOfWeek(a.AppointmentDate.Date))
                    .OrderBy(g => g.Key)
                    .Select(g => new DoctorInsightTrendItem
                    {
                        Label = $"{g.Key:dd MMM}",
                        AppointmentCount = g.Count(),
                        CompletedCount = g.Count(a => a.Status == AppointmentStatus.Tamamlandi)
                    })
                    .ToList();

            var statusBreakdown = Enum.GetValues(typeof(AppointmentStatus))
                .Cast<AppointmentStatus>()
                .Select(status => new DoctorStatusBreakdownItem
                {
                    StatusLabel = status.ToString(),
                    Count = filteredAppointments.Count(a => a.Status == status)
                })
                .ToList();

            var departmentBreakdown = filteredAppointments
                .GroupBy(a => a.Doctor?.Department?.Name ?? "Bilinmeyen")
                .Select(g => new DoctorDepartmentBreakdownItem
                {
                    DepartmentName = g.Key,
                    AppointmentCount = g.Count(),
                    UniquePatientCount = g.Select(a => a.PatientUserId ?? $"{a.PatientName}|{a.PatientSurname}|{a.PatientPhone}")
                        .Distinct()
                        .Count(),
                    CollectedRevenue = g.Where(a => a.IsCollected).Sum(GetFee)
                })
                .OrderByDescending(x => x.AppointmentCount)
                .ToList();

            var topPatients = filteredAppointments
                .GroupBy(a => $"{a.PatientName} {a.PatientSurname}".Trim())
                .Select(g => new DoctorTopPatientItem
                {
                    PatientName = g.Key,
                    AppointmentCount = g.Count()
                })
                .OrderByDescending(x => x.AppointmentCount)
                .ThenBy(x => x.PatientName)
                .Take(10)
                .ToList();

            var recentAppointments = filteredAppointments
                .OrderByDescending(a => a.AppointmentDate)
                .Take(20)
                .Select(a => new DoctorRecentAppointmentItem
                {
                    AppointmentId = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    PatientName = $"{a.PatientName} {a.PatientSurname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen",
                    Status = a.Status,
                    IsCollected = a.IsCollected
                })
                .ToList();

            var doctorRanking = rangeAppointments
                .GroupBy(a => new
                {
                    a.DoctorId,
                    DoctorName = a.Doctor == null
                        ? "Bilinmeyen Doktor"
                        : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen"
                })
                .Select(g => new DoctorRankingItem
                {
                    DoctorId = g.Key.DoctorId,
                    DoctorName = g.Key.DoctorName,
                    DepartmentName = g.Key.DepartmentName,
                    AppointmentCount = g.Count(),
                    CompletedCount = g.Count(a => a.Status == AppointmentStatus.Tamamlandi),
                    UniquePatientCount = g.Select(a => a.PatientUserId ?? $"{a.PatientName}|{a.PatientSurname}|{a.PatientPhone}")
                        .Distinct()
                        .Count(),
                    CollectedRevenue = g.Where(a => a.IsCollected).Sum(GetFee)
                })
                .OrderByDescending(x => x.AppointmentCount)
                .ThenByDescending(x => x.CollectedRevenue)
                .Take(12)
                .ToList();

            var model = new AdminDoctorInsightsViewModel
            {
                DoctorId = doctorId,
                Period = normalizedPeriod,
                StartDate = normalizedStart,
                EndDate = normalizedEnd,
                SelectedDoctorName = selectedDoctor == null
                    ? "Tum Doktorlar"
                    : $"{selectedDoctor.Title} {selectedDoctor.Name} {selectedDoctor.Surname}".Trim(),
                SelectedDepartmentName = selectedDoctor?.Department?.Name ?? "Tum Bolumler",
                TotalAppointments = totalAppointments,
                UniquePatientCount = uniquePatientCount,
                CompletedAppointments = completedAppointments,
                ApprovedAppointments = approvedAppointments,
                PendingAppointments = pendingAppointments,
                CancelledAppointments = cancelledAppointments,
                CollectionRate = totalAppointments == 0 ? 0 : Math.Round((decimal)collectedAppointments / totalAppointments * 100, 1),
                CompletionRate = totalAppointments == 0 ? 0 : Math.Round((decimal)completedAppointments / totalAppointments * 100, 1),
                EstimatedRevenue = estimatedRevenue,
                CollectedRevenue = collectedRevenue,
                DoctorOptions = doctorOptions,
                Trend = trend,
                StatusBreakdown = statusBreakdown,
                DepartmentBreakdown = departmentBreakdown,
                TopPatients = topPatients,
                RecentAppointments = recentAppointments,
                DoctorRanking = doctorRanking
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Tahsilat()
        {
            var now = DateTime.Now;
            var today = DateTime.Today;

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.AppointmentDate <= now)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            decimal GetFee(Appointment appointment)
            {
                var departmentName = appointment.Doctor?.Department?.Name;
                return appointment.Price ?? (departmentName != null && DepartmentPriceMap.TryGetValue(departmentName, out var fee)
                    ? fee
                    : DefaultAppointmentFee);
            }

            var pendingAppointments = appointments
                .Where(a => a.Status != AppointmentStatus.Iptal && !a.IsCollected)
                .ToList();

            var recentCollected = appointments
                .Where(a => a.IsCollected)
                .OrderByDescending(a => a.CollectedDate ?? a.AppointmentDate)
                .Take(12)
                .Select(a => new AccountingLedgerItem
                {
                    AppointmentId = a.Id,
                    RecordedDate = a.CreatedDate,
                    AppointmentDate = a.AppointmentDate,
                    PatientName = $"{a.PatientName} {a.PatientSurname}".Trim(),
                    DoctorName = a.Doctor == null ? "Bilinmeyen Doktor" : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen",
                    StatusLabel = "Tahsil edildi",
                    Amount = GetFee(a),
                    IsCollected = true,
                    IsAppointmentFinished = true
                })
                .ToList();

            var pendingItems = pendingAppointments
                .Select(a => new AccountingLedgerItem
                {
                    AppointmentId = a.Id,
                    RecordedDate = a.CreatedDate,
                    AppointmentDate = a.AppointmentDate,
                    PatientName = $"{a.PatientName} {a.PatientSurname}".Trim(),
                    DoctorName = a.Doctor == null ? "Bilinmeyen Doktor" : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "Bilinmeyen",
                    StatusLabel = a.AppointmentDate.Date < today ? "Gecikmis tahsilat" : "Bugun tahsil edilebilir",
                    Amount = GetFee(a),
                    IsCollected = false,
                    IsAppointmentFinished = true
                })
                .ToList();

            var topDepartment = pendingItems
                .GroupBy(x => x.DepartmentName)
                .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            var topDoctor = pendingItems
                .GroupBy(x => x.DoctorName)
                .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            return View(new TahsilatDashboardViewModel
            {
                PendingCount = pendingItems.Count,
                PendingTotal = pendingItems.Sum(x => x.Amount),
                OverdueCount = pendingItems.Count(x => x.AppointmentDate.Date < today),
                OverdueTotal = pendingItems.Where(x => x.AppointmentDate.Date < today).Sum(x => x.Amount),
                TodayCount = pendingItems.Count(x => x.AppointmentDate.Date == today),
                TodayTotal = pendingItems.Where(x => x.AppointmentDate.Date == today).Sum(x => x.Amount),
                CollectedTodayTotal = recentCollected.Where(x => x.RecordedDate.Date == today || x.AppointmentDate.Date == today).Sum(x => x.Amount),
                TopDepartmentName = topDepartment?.Name ?? "-",
                TopDepartmentAmount = topDepartment?.Total ?? 0,
                TopDoctorName = topDoctor?.Name ?? "-",
                TopDoctorAmount = topDoctor?.Total ?? 0,
                PendingItems = pendingItems,
                RecentCollectedItems = recentCollected
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkCollected(int id, DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, string? returnUrl = null)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Odeme islenecek kayit bulunamadi.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            if (appointment.Status == AppointmentStatus.Iptal)
            {
                TempData["ErrorMessage"] = "Iptal edilen randevu icin odeme islenemez.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Randevu bitmeden odeme yapildi olarak isaretlenemez.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            if (IsOfficialHoliday(appointment.AppointmentDate))
            {
                TempData["ErrorMessage"] = "Resmi tatil gunundeki randevu icin tahsilat yapilamaz.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            appointment.IsCollected = true;
            appointment.CollectedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Odeme yapildi olarak guncellendi.";
            return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
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
                return RedirectToAction(nameof(Tahsilat));
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Randevu bitmeden odeme kaydi degistirilemez.";
                return RedirectToAction(nameof(Tahsilat));
            }

            appointment.IsCollected = false;
            appointment.CollectedDate = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Odeme iptal edildi olarak guncellendi.";
            return RedirectToAction(nameof(Tahsilat));
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

            return View(model);
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

        private static bool IsOfficialHoliday(DateTime date)
        {
            return BuildHolidayMap(date.Year).ContainsKey(DateOnly.FromDateTime(date));
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

        private static DateTime StartOfWeek(DateTime date)
        {
            var day = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
            return date.Date.AddDays(1 - day);
        }
    }
}
