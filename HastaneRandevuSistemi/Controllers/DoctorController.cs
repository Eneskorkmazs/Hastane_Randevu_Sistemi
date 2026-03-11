using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HastaneRandevuSistemi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Rol yönetimi eklendi

        public DoctorController(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Doctor
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Doctors.Include(d => d.Department);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Doctor/Create
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Surname,DepartmentId")] Doctor doctor, string Email, string Password)
        {
            if (ModelState.IsValid)
            {
                // 1. Önce Identity (Giriş) Hesabını Oluşturuyoruz
                var user = new AppUser
                {
                    UserName = Email,
                    Email = Email,
                    Name = doctor.Name,
                    Surname = doctor.Surname,
                    EmailConfirmed = true
                };

                // Kullanıcıyı şifresiyle veritabanına (AspNetUsers) ekle
                var result = await _userManager.CreateAsync(user, Password);

                if (result.Succeeded)
                {
                    // 2. ROL ATAMA: Kullanıcıyı "Doktor" rolüne ekle
                    if (!await _roleManager.RoleExistsAsync("Doktor"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Doktor"));
                    }
                    await _userManager.AddToRoleAsync(user, "Doktor");

                    // 3. DOCTOR TABLOSUNA MÜHÜRLEME
                    // Formdan gelen doktor nesnesine, az önce oluşan kullanıcının ID'sini veriyoruz
                    // Not: Doctor modelinde UserId alanı olduğundan emin ol!
                    doctor.UserId = user.Id;

                    _context.Add(doctor);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                // Eğer Identity tarafında hata varsa (şifre zayıfsa vs.) buraya düşer
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // GET: Doctor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // POST: Doctor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Surname,Title,DepartmentId")] Doctor doctor)
        {
            if (id != doctor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // GET: Doctor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var doctor = await _context.Doctors.Include(d => d.Department).FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null) return NotFound();
            return View(doctor);
        }

        // POST: Doctor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                if (!string.IsNullOrWhiteSpace(doctor.UserId))
                {
                    var identityUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == doctor.UserId);
                    if (identityUser != null)
                    {
                        await _userManager.DeleteAsync(identityUser);
                    }
                }
                else
                {
                    var identityUser = await _userManager.Users.FirstOrDefaultAsync(
                        u => u.Name == doctor.Name && u.Surname == doctor.Surname && u.Email != null && u.Email.Contains("@hastane.com"));
                    if (identityUser != null)
                    {
                        await _userManager.DeleteAsync(identityUser);
                    }
                }

                // Sonra doktor kaydını siliyoruz
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }
    }
}
