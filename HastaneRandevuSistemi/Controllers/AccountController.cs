using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using HastaneRandevuSistemi.Services;
using HastaneRandevuSistemi.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;

namespace HastaneRandevuSistemi.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            EmailService emailService,
            ILogger<AccountController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                Surname = model.Surname,
                TC = model.TC,
                Telefon = model.Telefon,
                PhoneNumber = model.Telefon,
                DogumTarihi = model.DogumTarihi,
                Cinsiyet = model.Cinsiyet,
                Adres = model.Adres
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Hasta");
                await CreateNotificationAsync(
                    user.Id,
                    "Hesabınız oluşturuldu",
                    "Hasta profiliniz aktif edildi. Profilinizi tamamlayabilir ve hemen randevu oluşturabilirsiniz.",
                    "Hoş geldiniz",
                    "/Patient/Dashboard");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Dashboard", "Patient");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (!result.Succeeded)
                {
                    await SignOutAndClearUserAsync();
                    ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
                    return View(model);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    await SignOutAndClearUserAsync();
                    ModelState.AddModelError(string.Empty, "Hesap bulunamadı.");
                    return View(model);
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("AdminDashboard", "Home");
                }

                if (roles.Contains("Doktor"))
                {
                    if (string.Equals(model.SelectedRole, "Hasta", StringComparison.OrdinalIgnoreCase))
                    {
                        await SignOutAndClearUserAsync();
                        ModelState.AddModelError(string.Empty, "Bu hesap doktor hesabıdır. Lütfen doktor girişi seçerek tekrar deneyin.");
                        return View(model);
                    }

                    return RedirectToAction("DoctorDashboard", "Home");
                }

                if (string.Equals(model.SelectedRole, "Doktor", StringComparison.OrdinalIgnoreCase))
                {
                    await SignOutAndClearUserAsync();
                    ModelState.AddModelError(string.Empty, "Bu hesap hasta hesabıdır. Lütfen hasta girişi seçerek tekrar deneyin.");
                    return View(model);
                }

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Dashboard", "Patient");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Login sirasinda veritabani baglanti zaman asimi olustu.");
                ModelState.AddModelError(string.Empty, "Veritabanına bağlanılamadı. Lütfen internet bağlantınızı veya VPN ayarlarınızı kontrol edin.");
                return View(model);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Login sirasinda PostgreSQL baglanti hatasi olustu.");
                ModelState.AddModelError(string.Empty, "Veritabani baglantisi su an kullanilamiyor. Birazdan tekrar deneyin.");
                return View(model);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Login sirasinda gecici veritabani hatasi olustu.");
                ModelState.AddModelError(string.Empty, "Sistem geçici olarak veritabanına erişemiyor. Lütfen birazdan tekrar deneyin.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Lütfen e-posta adresinizi giriniz.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var link = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

                var body = $@"
                    <div style='font-family:Arial; padding:20px; border:1px solid #ddd; border-radius:10px;'>
                        <h2 style='color:#004e92;'>Şifre Sıfırlama Talebi</h2>
                        <p>Merhaba {user.Name},</p>
                        <p>Hesabınız için şifre sıfırlama talebinde bulundunuz. Aşağıdaki butona tıklayarak yeni şifrenizi belirleyebilirsiniz.</p>
                        <a href='{link}' style='background-color:#004e92; color:white; padding:10px 20px; text-decoration:none; border-radius:5px; display:inline-block; margin-top:10px;'>Şifremi Sıfırla</a>
                        <p style='margin-top:20px; font-size:12px; color:#666;'>Bu işlemi siz yapmadıysanız, bu e-postayı dikkate almayınız.</p>
                    </div>";

                var mailSent = await _emailService.SendEmailAsync(user.Email!, "HRS - Şifre Sıfırlama", body);
                if (!mailSent)
                {
                    _logger.LogWarning("ForgotPassword isteginde e-posta gonderilemedi. KullaniciId: {UserId}", user.Id);
                }
            }

            return View("ForgotPasswordConfirmation");
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError(string.Empty, "Geçersiz şifre sıfırlama anahtarı.");
                return View();
            }

            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await SignOutAndClearUserAsync();
            return RedirectToAction("Index", "Home");
        }

        private async Task SignOutAndClearUserAsync()
        {
            await _signInManager.SignOutAsync();
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
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







