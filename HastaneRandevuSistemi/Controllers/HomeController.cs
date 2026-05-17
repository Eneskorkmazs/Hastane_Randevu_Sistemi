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

        // 1. ANA SAYFA (VÄ°TRÄ°N) - Herkes GÃ¶rebilir
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            return View(await BuildHomeIndexViewModelAsync());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitHospitalReview(HomeIndexViewModel model)
        {
            if (model.Rating < 1 || model.Rating > 5)
            {
                ModelState.AddModelError(nameof(model.Rating), "Lütfen 1-5 arasında puan seçin.");
            }

            var reviewerName = (model.ReviewerName ?? string.Empty).Trim();
            var comment = (model.Comment ?? string.Empty).Trim();

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var hasExistingReview = await _context.HospitalReviews
                    .AsNoTracking()
                    .AnyAsync(r => r.UserId == user.Id);
                if (hasExistingReview)
                {
                    ModelState.AddModelError(string.Empty, "Bu hesap ile daha önce değerlendirme yaptınız. Her hesap yalnızca bir yorum bırakabilir.");
                }

                var fullName = $"{user.Name} {user.Surname}".Trim();
                reviewerName = string.IsNullOrWhiteSpace(fullName) ? (user.Email ?? reviewerName) : fullName;
            }

            if (string.IsNullOrWhiteSpace(reviewerName))
            {
                ModelState.AddModelError(nameof(model.ReviewerName), "Ad-soyad zorunludur.");
            }

            if (comment.Length > 500)
            {
                ModelState.AddModelError(nameof(model.Comment), "Yorum en fazla 500 karakter olabilir.");
            }

            if (!ModelState.IsValid)
            {
                var invalidViewModel = await BuildHomeIndexViewModelAsync();
                invalidViewModel.Rating = model.Rating;
                invalidViewModel.ReviewerName = model.ReviewerName ?? string.Empty;
                invalidViewModel.Comment = model.Comment;
                return View("Index", invalidViewModel);
            }

            _context.HospitalReviews.Add(new HospitalReview
            {
                Rating = model.Rating,
                ReviewerName = reviewerName,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment,
                UserId = user?.Id,
                CreatedAt = DateTime.Now
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsHospitalReviewUniqueViolation(ex))
            {
                ModelState.AddModelError(string.Empty, "Bu hesap ile daha önce değerlendirme yaptınız. Her hesap yalnızca bir yorum bırakabilir.");
                var invalidViewModel = await BuildHomeIndexViewModelAsync();
                invalidViewModel.Rating = model.Rating;
                invalidViewModel.ReviewerName = model.ReviewerName ?? string.Empty;
                invalidViewModel.Comment = model.Comment;
                return View("Index", invalidViewModel);
            }

            TempData["SuccessMessage"] = "Genel hastane değerlendirmeniz için teşekkür ederiz.";
            return RedirectToAction(nameof(Index));
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
            var pendingRevenue = allRevenueAppointments.Where(IsPaymentPending).Sum(GetFee);

            var pendingPayments = allRevenueAppointments
                .Where(IsPaymentPending)
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
                    a.Status == AppointmentStatus.Tamamlandi &&
                    !a.IsCollected),
                UniquePatientCount = registeredPatientCount + guestPatientCount,
                LatestNotifications = await _context.Notifications
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(7)
                    .ToListAsync(),
                LatestHospitalReviews = await _context.HospitalReviews
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(6)
                    .Select(r => new HomeReviewItemViewModel
                    {
                        ReviewerName = r.ReviewerName,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        AdminReply = r.AdminReply,
                        AdminReplyDate = r.AdminReplyDate
                    })
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
        public async Task<IActionResult> AdminAnalytics()
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
            var pendingRevenue = allRevenueAppointments.Where(IsPaymentPending).Sum(GetFee);

            return View(new DashboardViewModel
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
                    a.Status == AppointmentStatus.Tamamlandi &&
                    !a.IsCollected),
                UniquePatientCount = registeredPatientCount + guestPatientCount,
                DepartmentStats = await GetDepartmentStatsAsync(),
                WeeklyTrend = await GetWeeklyTrendAsync(today),
                TotalRevenue = totalRevenue,
                PendingRevenue = pendingRevenue
            });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminHospitalReviews(int? rating = null, string? search = null)
        {
            var normalizedSearch = (search ?? string.Empty).Trim();
            var reviewsQuery = _context.HospitalReviews.AsNoTracking();

            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
            {
                reviewsQuery = reviewsQuery.Where(r => r.Rating == rating.Value);
            }

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                reviewsQuery = reviewsQuery.Where(r =>
                    r.ReviewerName.Contains(normalizedSearch) ||
                    (r.Comment != null && r.Comment.Contains(normalizedSearch)));
            }

            var reviews = await reviewsQuery
                .OrderByDescending(r => r.CreatedAt)
                .Take(250)
                .Select(r => new AdminHospitalReviewItemViewModel
                {
                    Id = r.Id,
                    ReviewerName = r.ReviewerName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UserId = r.UserId,
                    AdminReply = r.AdminReply,
                    AdminReplyDate = r.AdminReplyDate
                })
                .ToListAsync();

            var allReviews = await _context.HospitalReviews.AsNoTracking().ToListAsync();
            var totalCount = allReviews.Count;
            var average = totalCount == 0 ? 0 : allReviews.Average(r => (double)r.Rating);

            var model = new AdminHospitalReviewsViewModel
            {
                TotalCount = totalCount,
                AverageRating = Math.Round(average, 1),
                OneStarCount = allReviews.Count(r => r.Rating == 1),
                TwoStarCount = allReviews.Count(r => r.Rating == 2),
                ThreeStarCount = allReviews.Count(r => r.Rating == 3),
                FourStarCount = allReviews.Count(r => r.Rating == 4),
                FiveStarCount = allReviews.Count(r => r.Rating == 5),
                SelectedRating = rating,
                Search = normalizedSearch,
                Reviews = reviews
            };

            Response.Cookies.Append(
                "__HRS_ADMIN_REVIEWS_LAST_SEEN_UTC_MS",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps
                });

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHospitalReview(int id)
        {
            var review = await _context.HospitalReviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Silinecek yorum bulunamadÄ±.";
                return RedirectToAction(nameof(AdminHospitalReviews));
            }

            _context.HospitalReviews.Remove(review);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Yorum silindi.";
            return RedirectToAction(nameof(AdminHospitalReviews));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyHospitalReview(int id, string reply)
        {
            var review = await _context.HospitalReviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
            {
                TempData["ErrorMessage"] = "YanÄ±tlanacak yorum bulunamadÄ±.";
                return RedirectToAction(nameof(AdminHospitalReviews));
            }

            var trimmedReply = (reply ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedReply))
            {
                TempData["ErrorMessage"] = "YanÄ±t boÅŸ olamaz.";
                return RedirectToAction(nameof(AdminHospitalReviews));
            }

            if (trimmedReply.Length > 1000)
            {
                TempData["ErrorMessage"] = "YanÄ±t en fazla 1000 karakter olabilir.";
                return RedirectToAction(nameof(AdminHospitalReviews));
            }

            review.AdminReply = trimmedReply;
            review.AdminReplyDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "YanÄ±t baÅŸarÄ±yla gÃ¶nderildi.";
            return RedirectToAction(nameof(AdminHospitalReviews));
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
        public async Task<IActionResult> AdminServiceAnalysis(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null)
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var normalizedFrom = fromDate?.Date ?? DateTime.Today.AddDays(-30);
            var normalizedTo = toDate?.Date ?? DateTime.Today;

            var query = _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.AppointmentDate >= normalizedFrom && a.AppointmentDate < normalizedTo.AddDays(1));

            if (departmentId.HasValue)
            {
                query = query.Where(a => a.Doctor != null && a.Doctor.DepartmentId == departmentId.Value);
            }

            var appointments = await query.ToListAsync();

            var viewModel = new AdminServiceAnalysisViewModel
            {
                FromDate = normalizedFrom,
                ToDate = normalizedTo,
                DepartmentId = departmentId,
                TotalAppointments = appointments.Count,
                CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Tamamlandi),
                PendingAppointments = appointments.Count(a => a.Status == AppointmentStatus.Bekliyor || a.Status == AppointmentStatus.Onaylandi),
                CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Iptal),
                
                StatusBreakdown = Enum.GetValues(typeof(AppointmentStatus))
                    .Cast<AppointmentStatus>()
                    .Select(s => new ServiceStatusItem
                    {
                        StatusLabel = s.ToString(),
                        Count = appointments.Count(a => a.Status == s),
                        Color = s switch {
                            AppointmentStatus.Tamamlandi => "#198754",
                            AppointmentStatus.Iptal => "#dc3545",
                            AppointmentStatus.Onaylandi => "#0d6efd",
                            _ => "#ffc107"
                        }
                    }).ToList(),

                DepartmentVolume = appointments
                    .GroupBy(a => a.Doctor?.Department?.Name ?? "Bilinmeyen")
                    .Select(g => new ServiceDepartmentItem {
                        DepartmentName = g.Key,
                        AppointmentCount = g.Count(),
                        SharePercent = appointments.Count > 0 ? (double)g.Count() / appointments.Count * 100 : 0
                    })
                    .OrderByDescending(x => x.AppointmentCount)
                    .ToList(),

                DailyVolumeTrend = appointments
                    .GroupBy(a => a.AppointmentDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new ServiceTrendPoint {
                        DateLabel = g.Key.ToString("dd MMM"),
                        Count = g.Count()
                    }).ToList(),

                PeakHours = appointments
                    .GroupBy(a => a.AppointmentDate.Hour)
                    .Select(g => new ServicePeakHourItem {
                        Hour = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Hour)
                    .ToList(),

                DepartmentOptions = await _context.Departments
                    .OrderBy(d => d.Name)
                    .Select(d => new SelectListItem {
                        Value = d.Id.ToString(),
                        Text = d.Name,
                        Selected = departmentId.HasValue && d.Id == departmentId.Value
                    }).ToListAsync()
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAccounting(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, int? doctorId = null)
        {
            await PopulateAccountingFiltersAsync(departmentId, doctorId);
            var model = await BuildAdminAccountingViewModel(fromDate, toDate, departmentId, doctorId);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAccountingDepartments(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, int? doctorId = null)
        {
            await PopulateAccountingFiltersAsync(departmentId, doctorId);
            var model = await BuildAdminAccountingViewModel(fromDate, toDate, departmentId, doctorId);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAccountingDoctors(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, int? doctorId = null)
        {
            await PopulateAccountingFiltersAsync(departmentId, doctorId);
            var model = await BuildAdminAccountingViewModel(fromDate, toDate, departmentId, doctorId);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAccountingTransactions(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, int? doctorId = null)
        {
            await PopulateAccountingFiltersAsync(departmentId, doctorId);
            var model = await BuildAdminAccountingViewModel(fromDate, toDate, departmentId, doctorId);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAccountingPending(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, int? doctorId = null)
        {
            await PopulateAccountingFiltersAsync(departmentId, doctorId);
            var model = await BuildAdminAccountingViewModel(fromDate, toDate, departmentId, doctorId);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAccountingStats(DateTime? fromDate = null, DateTime? toDate = null, int? departmentId = null, int? doctorId = null)
        {
            await PopulateAccountingFiltersAsync(departmentId, doctorId);
            var model = await BuildAdminAccountingViewModel(fromDate, toDate, departmentId, doctorId);
            return View(model);
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
                new() { Value = string.Empty, Text = "Tüm Doktorlar", Selected = !doctorId.HasValue }
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
                    DoctorName = a.Doctor != null ? $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim() : "Bilinmeyen",
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
                    ? "Tüm Doktorlar"
                    : $"{selectedDoctor.Title} {selectedDoctor.Name} {selectedDoctor.Surname}".Trim(),
                SelectedDepartmentName = selectedDoctor?.Department?.Name ?? "Tüm Bölümler",
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
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

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
                .Where(IsPaymentPending)
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

            // Toplam kasa: tüm zamanlar tahsil edilen
            var allCollectedTotal = appointments
                .Where(a => a.IsCollected)
                .Sum(GetFee);

            ViewBag.AllCollectedTotal = allCollectedTotal;

            return View(new TahsilatDashboardViewModel
            {
                PendingCount = pendingItems.Count,
                PendingTotal = pendingItems.Sum(x => x.Amount),
                OverdueCount = pendingItems.Count(x => x.AppointmentDate.Date < today),
                OverdueTotal = pendingItems.Where(x => x.AppointmentDate.Date < today).Sum(x => x.Amount),
                TodayCount = pendingItems.Count(x => x.AppointmentDate.Date == today),
                TodayTotal = pendingItems.Where(x => x.AppointmentDate.Date == today).Sum(x => x.Amount),
                CollectedTodayTotal = recentCollected.Where(x => x.RecordedDate.Date == today).Sum(x => x.Amount),
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
                TempData["ErrorMessage"] = "Ödeme işlenecek kayıt bulunamadı.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            if (appointment.Status == AppointmentStatus.Iptal)
            {
                TempData["ErrorMessage"] = "İptal edilen randevu için ödeme işlenemez.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Randevu bitmeden ödeme yapıldı olarak işaretlenemez.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            if (IsOfficialHoliday(appointment.AppointmentDate))
            {
                TempData["ErrorMessage"] = "Resmi tatil günündeki randevu için tahsilat yapılamaz.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            if (appointment.Status != AppointmentStatus.Tamamlandi)
            {
                appointment.Status = AppointmentStatus.Tamamlandi;
            }

            appointment.IsCollected = true;
            appointment.CollectedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ödeme yapıldı olarak güncellendi.";
            return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAllCollected(string? returnUrl = null)
        {
            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var now = DateTime.Now;
            var appointments = await _context.Appointments
                .Where(a => a.AppointmentDate <= now
                    && a.Status == AppointmentStatus.Tamamlandi
                    && !a.IsCollected)
                .ToListAsync();

            var eligibleAppointments = appointments
                .Where(a => !IsOfficialHoliday(a.AppointmentDate))
                .ToList();

            if (eligibleAppointments.Count == 0)
            {
                TempData["InfoMessage"] = "Toplu tahsilat için uygun, randevusu bitmiş ödeme bulunamadı.";
                return !string.IsNullOrEmpty(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Tahsilat));
            }

            foreach (var appointment in eligibleAppointments)
            {
                appointment.IsCollected = true;
                appointment.CollectedDate = now;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{eligibleAppointments.Count} adet bitmiş randevunun ödemesi toplu olarak alındı.";
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
                TempData["ErrorMessage"] = "Ödeme iptal edilecek kayıt bulunamadı.";
                return RedirectToAction(nameof(Tahsilat));
            }

            if (appointment.AppointmentDate > DateTime.Now)
            {
                TempData["ErrorMessage"] = "Randevu bitmeden ödeme kaydı değiştirilemez.";
                return RedirectToAction(nameof(Tahsilat));
            }

            appointment.IsCollected = false;
            appointment.CollectedDate = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ödeme iptal edildi olarak güncellendi.";
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
            TempData["SuccessMessage"] = $"{users.Count} kullanıcıya duyuru gönderildi.";
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

        /// <summary>
        /// Sıra Takip Ekranı — Bugünkü randevuların doktor bazlı sırasını ve tahmini bekleme süresini gösterir.
        /// Herkese açık (giriş gerekmez), admin göremez.
        /// </summary>
        [AllowAnonymous]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> QueueStatus(int? departmentId = null)
        {
            // Admin bu sayfayÄ± gÃ¶remez
            if (User.IsInRole("Admin"))
                return RedirectToAction("AdminDashboard");

            await AppointmentStatusSync.CompleteExpiredAppointmentsAsync(_context);

            var today = DateTime.Today;
            var now = DateTime.Now;

            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .ToListAsync();

            var todayQuery = _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a => a.AppointmentDate.Date == today
                    && a.Status != AppointmentStatus.Iptal);

            if (departmentId.HasValue)
            {
                todayQuery = todayQuery.Where(a => a.Doctor != null && a.Doctor.DepartmentId == departmentId.Value);
            }

            var todayAppointments = await todayQuery
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            // Tahmini bekleme: her randevu ortalama 15 dakika
            const int avgMinutesPerAppointment = 15;

            // Doktor bazlÄ± sÄ±ra hesaplama:
            // Her doktor iÃ§in kendi sÄ±rasÄ±nÄ± ayrÄ± tut.
            // SÄ±ra = o doktorda Ã¶nÃ¼ndeki tamamlanmamÄ±ÅŸ randevu sayÄ±sÄ± (saate gÃ¶re).
            // Tamamlanan randevular sÄ±radan dÃ¼ÅŸer.
            var queueItems = todayAppointments.Select(a =>
            {
                var isCompleted = a.Status == AppointmentStatus.Tamamlandi;

                // Bu doktorda, bu randevudan Ã¶nce gelen ve henÃ¼z tamamlanmamÄ±ÅŸ randevular
                var aheadInDoctorQueue = todayAppointments
                    .Count(x => x.DoctorId == a.DoctorId
                        && x.AppointmentDate < a.AppointmentDate
                        && x.Status != AppointmentStatus.Tamamlandi
                        && x.Status != AppointmentStatus.Iptal);

                // Bu doktorda kaÃ§Ä±ncÄ± sÄ±rada? (tamamlananlar dahil tÃ¼m sÄ±ra numarasÄ±)
                var positionInDoctor = todayAppointments
                    .Count(x => x.DoctorId == a.DoctorId
                        && x.AppointmentDate <= a.AppointmentDate
                        && x.Id <= a.Id);

                // Åu an muayenede mi?
                // Doktorda Ã¶nÃ¼nde kimse kalmamÄ±ÅŸ + randevu saati geÃ§miÅŸ + tamamlanmamÄ±ÅŸ
                var isCurrent = !isCompleted
                    && aheadInDoctorQueue == 0
                    && a.AppointmentDate <= now;

                var isPending = !isCompleted && !isCurrent;

                // Tahmini bekleme = Ã¶nÃ¼ndeki tamamlanmamÄ±ÅŸ kiÅŸi sayÄ±sÄ± Ã— 15 dk
                var estimatedWaitMinutes = isCompleted ? 0 : aheadInDoctorQueue * avgMinutesPerAppointment;

                return new QueueItemViewModel
                {
                    AppointmentId = a.Id,
                    PatientName = $"{a.PatientName} {a.PatientSurname}".Trim(),
                    AppointmentTime = a.AppointmentDate,
                    DoctorName = a.Doctor == null ? "-" : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim(),
                    DepartmentName = a.Doctor?.Department?.Name ?? "-",
                    Status = a.Status,
                    IsCompleted = isCompleted,
                    IsCurrent = isCurrent,
                    IsPending = isPending,
                    QueuePosition = positionInDoctor,
                    EstimatedWaitMinutes = estimatedWaitMinutes
                };
            }).ToList();

            var completedCount = queueItems.Count(x => x.IsCompleted);
            var pendingCount = queueItems.Count(x => x.IsPending);
            var currentItem = queueItems.FirstOrDefault(x => x.IsCurrent);

            ViewBag.Departments = departments;
            ViewBag.SelectedDepartmentId = departmentId;
            ViewBag.CompletedCount = completedCount;
            ViewBag.PendingCount = pendingCount;
            ViewBag.CurrentItem = currentItem;
            ViewBag.Today = today.ToString("dd MMMM yyyy, dddd", new System.Globalization.CultureInfo("tr-TR"));

            return View(queueItems);
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

        private async Task PopulateAccountingFiltersAsync(int? departmentId, int? doctorId)
        {
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
        }

        private async Task<AdminAccountingViewModel> BuildAdminAccountingViewModel(DateTime? fromDate, DateTime? toDate, int? departmentId, int? doctorId)
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

            var estimatedRevenue = appointments
                .Where(a => a.Status != AppointmentStatus.Iptal)
                .Sum(GetAppointmentFee);

            var collectedRevenue = appointments
                .Where(a => a.IsCollected)
                .Sum(GetAppointmentFee);

            var pendingRevenue = appointments
                .Where(IsPaymentPending)
                .Sum(GetAppointmentFee);

            var cancelledRevenue = appointments
                .Where(a => a.Status == AppointmentStatus.Iptal)
                .Sum(GetAppointmentFee);

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
                    EstimatedRevenue = g.Where(x => x.Status != AppointmentStatus.Iptal).Sum(GetAppointmentFee),
                    CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetAppointmentFee),
                    PendingRevenue = g.Where(IsPaymentPending).Sum(GetAppointmentFee)
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
                    EstimatedRevenue = g.Where(x => x.Status != AppointmentStatus.Iptal).Sum(GetAppointmentFee),
                    CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetAppointmentFee),
                    PendingRevenue = g.Where(IsPaymentPending).Sum(GetAppointmentFee),
                    CancelledRevenue = g.Where(x => x.Status == AppointmentStatus.Iptal).Sum(GetAppointmentFee)
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
                var selectedDepartment = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId.Value);

                if (selectedDepartment != null)
                {
                    var departmentAppointments = appointments.Where(a => a.Doctor?.DepartmentId == departmentId.Value).ToList();

                    var topDoctorInSelectedDepartment = departmentAppointments
                        .GroupBy(a => a.Doctor == null
                            ? "Bilinmeyen Doktor"
                            : $"{a.Doctor.Title} {a.Doctor.Name} {a.Doctor.Surname}".Trim())
                        .Select(g => new
                        {
                            DoctorName = g.Key,
                            AppointmentCount = g.Count(),
                            Revenue = g.Where(x => x.IsCollected).Sum(GetAppointmentFee)
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
                        CollectedRevenue = departmentAppointments.Where(a => a.IsCollected).Sum(GetAppointmentFee),
                        PendingRevenue = departmentAppointments.Where(IsPaymentPending).Sum(GetAppointmentFee),
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
                            CollectedRevenue = g.Where(a => a.IsCollected).Sum(GetAppointmentFee),
                            PendingRevenue = g.Where(IsPaymentPending).Sum(GetAppointmentFee)
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
                                        => "Ödeme iadesi yapıldı / İptal edildi",
                                AppointmentStatus.Iptal => "İptal edildi",
                                AppointmentStatus.Tamamlandi => "Ödeme Bekliyor",
                                AppointmentStatus.Onaylandi => "Randevu Bekliyor",
                                _ => "Planlandi"
                            },
                        Amount = GetAppointmentFee(a),
                        IsCollected = a.IsCollected,
                        IsAppointmentFinished = a.AppointmentDate <= DateTime.Now,
                        IsHoliday = isHoliday,
                        HolidayLabel = holidayLabel ?? string.Empty
                    };
                })
                .ToList();

            var pendingCollectionQueue = appointments
                .Where(IsPaymentPending)
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
                        StatusLabel = isHoliday ? $"Resmi Tatil ({holidayLabel})" : "Ödeme Bekliyor",
                        Amount = GetAppointmentFee(a),
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
                        CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetAppointmentFee),
                        PendingRevenue = g.Where(IsPaymentPending).Sum(GetAppointmentFee)
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
                        CollectedRevenue = g.Where(x => x.IsCollected).Sum(GetAppointmentFee),
                        PendingRevenue = g.Where(IsPaymentPending).Sum(GetAppointmentFee)
                    })
                    .ToList();

            return new AdminAccountingViewModel
            {
                FromDate = normalizedFrom,
                ToDate = normalizedTo,
                DepartmentId = departmentId,
                DoctorId = doctorId,
                TotalAppointments = appointments.Count,
                CompletedAppointments = appointments.Count(a => a.IsCollected),
                PendingAppointments = appointments.Count(IsPaymentPending),
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
            };
        }

        private decimal GetAppointmentFee(Appointment appointment)
        {
            var departmentName = appointment.Doctor?.Department?.Name;
            return appointment.Price ?? (departmentName != null && DepartmentPriceMap.TryGetValue(departmentName, out var fee)
                ? fee
                : DefaultAppointmentFee);
        }

        private static bool IsPaymentPending(Appointment appointment)
        {
            return appointment.Status == AppointmentStatus.Tamamlandi && !appointment.IsCollected;
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

        private static bool IsHospitalReviewUniqueViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains("UNIQUE constraint failed: HospitalReviews.UserId", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UX_HospitalReviews_UserId_NotNull", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            var day = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
            return date.Date.AddDays(1 - day);
        }

        private async Task<HomeIndexViewModel> BuildHomeIndexViewModelAsync()
        {
            var reviewsQuery = _context.HospitalReviews.AsNoTracking();
            var totalCount = await reviewsQuery.CountAsync();
            var averageRating = totalCount == 0
                ? 0
                : await reviewsQuery.AverageAsync(r => (double)r.Rating);
            var currentUser = await _userManager.GetUserAsync(User);
            var hasCurrentUserReview = currentUser != null
                && await _context.HospitalReviews.AsNoTracking().AnyAsync(r => r.UserId == currentUser.Id);

            var recentReviews = await reviewsQuery
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .Select(r => new HomeReviewItemViewModel
                {
                    ReviewerName = r.ReviewerName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    AdminReply = r.AdminReply,
                    AdminReplyDate = r.AdminReplyDate
                })
                .ToListAsync();

            return new HomeIndexViewModel
            {
                TotalReviewCount = totalCount,
                AverageRating = Math.Round(averageRating, 1),
                HasCurrentUserReview = hasCurrentUserReview,
                RecentReviews = recentReviews
            };
        }

        // --- DOKTOR REÇETE İŞLEMLERİ ---

        [Authorize(Roles = "Doktor")]
        [HttpGet]
        public async Task<IActionResult> DoctorPrescription(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(DoctorDashboard));
            }

            var user = await _userManager.GetUserAsync(User);
            if (appointment.Doctor == null || appointment.Doctor.UserId != user?.Id)
            {
                 return Forbid();
            }
            var doctor = appointment.Doctor;

            // Tarihi gelmemiş randevulara reçete yazılamaz
            if (appointment.AppointmentDate > DateTime.Now.AddMinutes(5))
            {
                TempData["ErrorMessage"] = "Tamamlanmamış randevu. Randevu saati henüz gelmediği için reçete oluşturulamaz.";
                return RedirectToAction(nameof(DoctorDashboard));
            }

            var model = new HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel
            {
                AppointmentId = appointment.Id,
                PatientName = appointment.PatientName,
                PatientSurname = appointment.PatientSurname,
                DoctorName = $"{doctor.Title} {doctor.Name} {doctor.Surname}".Trim(),
                DepartmentName = doctor.Department?.Name ?? string.Empty,
                PrescriptionDate = appointment.PrescriptionCreatedAt ?? DateTime.Now,
                Diagnosis = appointment.PrescriptionDiagnosis ?? string.Empty,
                Medications = appointment.PrescriptionMedications ?? string.Empty,
                Notes = appointment.PrescriptionNotes
            };

            return View(model);
        }

        [Authorize(Roles = "Doktor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorPrescription(HastaneRandevuSistemi.ViewModels.PrescriptionDraftViewModel model)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(DoctorDashboard));
            }

            var user = await _userManager.GetUserAsync(User);
            if (appointment.Doctor == null || appointment.Doctor.UserId != user?.Id) return Forbid();
            var doctor = appointment.Doctor;

            // Tarihi gelmemiş randevulara reçete yazılamaz (Gelecek randevular)
            if (appointment.AppointmentDate > DateTime.Now.AddMinutes(5))
            {
                TempData["ErrorMessage"] = "Tamamlanmamış randevu. Randevu süreci henüz başlamadığı için reçete kaydedilemez.";
                return RedirectToAction(nameof(DoctorDashboard));
            }

            if (!ModelState.IsValid)
            {
                model.DoctorName = $"{doctor.Title} {doctor.Name} {doctor.Surname}".Trim();
                model.DepartmentName = doctor.Department?.Name ?? string.Empty;
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
            
            // Eğer randevu henüz tamamlanmamışsa, reçete yazıldığında otomatik tamamla
            if (appointment.Status != AppointmentStatus.Tamamlandi)
            {
                appointment.Status = AppointmentStatus.Tamamlandi;
            }
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reçete başarıyla kaydedildi. Sekreterlik tarafından hastaya iletilecektir.";
            return RedirectToAction(nameof(DoctorDashboard));
        }
    }
}

