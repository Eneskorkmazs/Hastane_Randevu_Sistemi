using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AppointmentController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var appointmentsQuery = _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d!.Department)
                .AsQueryable();

            if (User.IsInRole("Doktor"))
            {
                var doctorId = await GetCurrentDoctorIdAsync();
                appointmentsQuery = doctorId.HasValue
                    ? appointmentsQuery.Where(a => a.DoctorId == doctorId.Value)
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

            return View(await appointmentsQuery.OrderByDescending(a => a.AppointmentDate).ToListAsync());
        }

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
                ModelState.AddModelError(nameof(appointment.AppointmentDate), "Randevu tarihi ileri bir zaman olmalıdır.");
            }

            if (appointment.AppointmentDate.Minute != 0 || appointment.AppointmentDate.Hour < 9 || appointment.AppointmentDate.Hour > 16)
            {
                ModelState.AddModelError(nameof(appointment.AppointmentDate), "Randevular 09:00 - 16:00 arasındaki saat başlarında oluşturulabilir.");
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

        [Authorize(Roles = "Admin,Doktor")]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Doktor")]
        public async Task<IActionResult> Approve(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
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
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Doktor,Hasta")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Hasta"))
            {
                var patientUser = await _userManager.GetUserAsync(User);
                var isOwner = patientUser != null && (
                    appointment.PatientUserId == patientUser.Id ||
                    (appointment.PatientUserId == null && appointment.PatientName == patientUser.Name && appointment.PatientSurname == patientUser.Surname));

                if (!isOwner || appointment.AppointmentDate <= DateTime.Now || appointment.Status is AppointmentStatus.Iptal or AppointmentStatus.Tamamlandi)
                {
                    TempData["ErrorMessage"] = "Bu randevu iptal edilemez.";
                    return RedirectToAction(nameof(Index));
                }
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

        [Authorize(Roles = "Admin,Doktor")]
        public async Task<IActionResult> Complete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
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
        public IActionResult GetTakenSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var selectedDate))
            {
                return BadRequest();
            }

            var taken = _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == selectedDate.Date && a.Status != AppointmentStatus.Iptal)
                .Select(a => a.AppointmentDate.ToString("HH:mm"))
                .ToList();

            return Json(taken);
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
