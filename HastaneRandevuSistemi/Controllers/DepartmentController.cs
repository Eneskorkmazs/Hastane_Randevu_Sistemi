using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HastaneRandevuSistemi.Data;
using HastaneRandevuSistemi.Models;
using Microsoft.AspNetCore.Authorization;

namespace HastaneRandevuSistemi.Controllers
{
    [Authorize(Roles = "Admin")] // Sadece Admin
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Department
        public async Task<IActionResult> Index()
        {
            return View(await _context.Departments.ToListAsync());
        }

        // GET: Department/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Department/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] Department department)
        {
            department.Name = department.Name?.Trim() ?? string.Empty;
            department.Description = department.Description?.Trim();

            if (!string.IsNullOrWhiteSpace(department.Name))
            {
                var exists = await _context.Departments.AnyAsync(d => d.Name.ToLower() == department.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError(nameof(department.Name), "Bu isimde bir poliklinik zaten mevcut.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Poliklinik basariyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: Department/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();
            return View(department);
        }

        // POST: Department/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Department department)
        {
            if (id != department.Id) return NotFound();

            department.Name = department.Name?.Trim() ?? string.Empty;
            department.Description = department.Description?.Trim();

            if (!string.IsNullOrWhiteSpace(department.Name))
            {
                var exists = await _context.Departments.AnyAsync(d => d.Id != department.Id && d.Name.ToLower() == department.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError(nameof(department.Name), "Bu isimde bir poliklinik zaten mevcut.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Poliklinik bilgileri guncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: Department/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            // DOKTORLARI DA INCLUDE EDEREK ÇEKİYORUZ
            var department = await _context.Departments
                .Include(d => d.Doctors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (department == null) return NotFound();

            return View(department);
        }

        // POST: Department/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Sadece bölümü değil, ona bağlı doktorları da listeye dahil ederek çekiyoruz
            var department = await _context.Departments
                .Include(d => d.Doctors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (department != null)
            {
                // Eğer bu bölüme kayıtlı en az bir doktor varsa
                if (department.Doctors?.Any() == true)
                {
                    // Kullanıcıya hata mesajı gönder ve silme işlemini yapmadan Index'e dön
                    TempData["ErrorMessage"] = $"'{department.Name}' polikliniği silinemez! İçerisinde kayıtlı doktorlar bulunmaktadır.";
                    return RedirectToAction(nameof(Index));
                }

                // Eğer doktor yoksa silme işlemini güvenle yapabiliriz
                _context.Departments.Remove(department);    
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Poliklinik başarıyla silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // === EKSİK OLAN METOT BURAYA EKLENDİ ===
        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
