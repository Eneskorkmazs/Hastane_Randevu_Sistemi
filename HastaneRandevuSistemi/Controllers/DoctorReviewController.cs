using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HastaneRandevuSistemi.Controllers
{
    public class DoctorReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DoctorReviewController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Doktor profil + değerlendirme sayfası — herkese açık
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Profile(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null) return NotFound();

            var reviews = await _context.DoctorReviews
                .Where(r => r.DoctorId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .ToListAsync();

            var avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;

            // Giriş yapmış kullanıcı daha önce yorum yaptı mı?
            var currentUser = await _userManager.GetUserAsync(User);
            var hasReviewed = currentUser != null &&
                await _context.DoctorReviews.AnyAsync(r => r.DoctorId == id && r.UserId == currentUser.Id);

            ViewBag.Doctor = doctor;
            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = Math.Round(avgRating, 1);
            ViewBag.ReviewCount = reviews.Count;
            ViewBag.HasReviewed = hasReviewed;
            ViewBag.CurrentUser = currentUser;

            return View();
        }

        /// <summary>
        /// Doktor değerlendirme gönder — giriş gerekli
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int doctorId, int rating, string? comment)
        {
            TempData["ErrorMessage"] = "Doktor değerlendirme özelliği şu an kapalıdır.";
            return RedirectToAction(nameof(Profile), new { id = doctorId });
        }

        /// <summary>
        /// Admin: yorum sil
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int doctorId)
        {
            var review = await _context.DoctorReviews.FindAsync(id);
            if (review != null)
            {
                _context.DoctorReviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Yorum silindi.";
            }
            return RedirectToAction(nameof(Profile), new { id = doctorId });
        }
    }
}
