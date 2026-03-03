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
            // Admin tüm istatistikleri görür
            var model = new DashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Bekliyor),
                TodaysAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today)
            };

            return View(model); // Views/Home/AdminDashboard.cshtml sayfasýna gider
        }

        // 3. DOKTOR DASHBOARD - Sadece Doktor Görebilir
        [Authorize(Roles = "Doktor")]
        public async Task<IActionResult> DoctorDashboard()
        {
            // Giriþ yapan kullanýcýnýn bilgilerini al
            var user = await _userManager.GetUserAsync(User);

            // Varsayýlan boþ deðerler
            int myAppointments = 0;
            int myPending = 0;
            int myToday = 0;

            if (user != null)
            {
                // Giriþ yapan kullanýcýnýn Adý ve Soyadý ile eþleþen Doktor kaydýný bul
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Name == user.Name && d.Surname == user.Surname);

                if (doctor != null)
                {
                    // Sadece BU DOKTORA ait verileri say
                    myAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == doctor.Id);

                    myPending = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.Status == AppointmentStatus.Bekliyor);

                    myToday = await _context.Appointments
                        .CountAsync(a => a.DoctorId == doctor.Id && a.AppointmentDate.Date == DateTime.Today);
                }
            }

            // Dashboard modelini doktora özel doldur
            var model = new DashboardViewModel
            {
                TotalAppointments = myAppointments,    // Toplam Randevusu
                PendingAppointments = myPending,       // Onay Bekleyenleri
                TodaysAppointments = myToday,          // Bugünün Ýþleri
                // Diðer genel istatistikleri doktorun görmesine gerek yok (0 býrakabiliriz veya doldurabiliriz)
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
    }
}