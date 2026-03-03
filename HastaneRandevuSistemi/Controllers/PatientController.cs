using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public PatientController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var appointments = await GetPatientAppointmentsQuery(user)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            var now = DateTime.Now;
            var model = new PatientDashboardViewModel
            {
                FullName = $"{user.Name} {user.Surname}".Trim(),
                Email = user.Email,
                Telefon = user.Telefon ?? user.PhoneNumber,
                TC = user.TC,
                DogumTarihi = user.DogumTarihi,
                Cinsiyet = user.Cinsiyet,
                UpcomingAppointmentsCount = appointments.Count(a => a.AppointmentDate >= now && a.Status != AppointmentStatus.Iptal),
                CompletedAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Tamamlandi),
                CancelledAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Iptal),
                UnreadNotificationsCount = notifications.Count(n => !n.IsRead),
                UpcomingAppointments = appointments
                    .Where(a => a.AppointmentDate >= now && a.Status != AppointmentStatus.Iptal)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(5)
                    .ToList(),
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

        private IQueryable<Appointment> GetPatientAppointmentsQuery(AppUser user)
        {
            return _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .Where(a =>
                    a.PatientUserId == user.Id ||
                    (a.PatientUserId == null && a.PatientName == user.Name && a.PatientSurname == user.Surname));
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
