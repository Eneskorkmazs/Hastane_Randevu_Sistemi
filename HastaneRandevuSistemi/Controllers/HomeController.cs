using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HastaneRandevuSistemi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        // UserManager'ý ekledik ki doktorun kim olduðunu bulabilelim
        private readonly UserManager<AppUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // 1. ANA SAYFA (VÝTRÝN) - Herkes Görebilir
        public IActionResult Index()
        {
            return View();
        }

        // 2. ADMIN DASHBOARD - Sadece Admin Görebilir
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var model = new DashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Bekliyor),
                ApprovedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Onaylandi),
                CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Tamamlandi),
                ThisWeekAppointments = await GetWeekAppointmentCountAsync(DateTime.Today),
                TodaysAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
                LatestNotifications = await _context.Notifications
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(7)
                    .ToListAsync()
            };

            return View(model); // Views/Home/AdminDashboard.cshtml sayfasýna gider
        }

        // 3. DOKTOR DASHBOARD - Sadece Doktor Görebilir
        [Authorize(Roles = "Doktor")]
        public async Task<IActionResult> DoctorDashboard()
        {
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
                TodaysAppointments = myToday,
                ThisWeekAppointments = thisWeek,
                UpcomingAppointments = upcomingAppointments,
                TotalDoctors = 0,
                TotalDepartments = 0
            };

            return View(model); // Views/Home/DoctorDashboard.cshtml sayfasýna gider
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
    }
}
