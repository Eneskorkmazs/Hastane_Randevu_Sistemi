using HastaneRandevuSistemi.Services;
using HastaneRandevuSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HastaneRandevuSistemi.Controllers
{
    [AllowAnonymous]
    public class SymptomController : Controller
    {
        private readonly ISymptomCheckerService _symptomCheckerService;

        public SymptomController(ISymptomCheckerService symptomCheckerService)
        {
            _symptomCheckerService = symptomCheckerService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new SymptomViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Analyze(SymptomViewModel model)
        {
            if (model.SelectedSymptoms == null || model.SelectedSymptoms.Count == 0 || model.SelectedSymptoms.All(string.IsNullOrWhiteSpace))
            {
                ModelState.AddModelError(nameof(model.SelectedSymptoms), "Lütfen en az bir semptom seçiniz.");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var selectedSymptoms = model.SelectedSymptoms ?? new List<string>();
            var labels = SymptomViewModel.AvailableSymptoms
                .Where(option => selectedSymptoms.Contains(option.Key))
                .Select(option => option.Label)
                .ToList();

            var suggestions = _symptomCheckerService.Analyze(selectedSymptoms);
            return View("Result", new SymptomResultViewModel
            {
                SelectedSymptomLabels = labels,
                Suggestions = suggestions,
                WarningMessage = suggestions.Count == 0
                    ? "Seçtiğiniz semptomlara göre güçlü bir bölüm eşleşmesi bulunamadı. Genel değerlendirme için Dahiliye bölümünden randevu alabilirsiniz."
                    : "Bu öneriler tıbbi tanı değildir; acil veya şiddetli belirtilerde en yakın sağlık kuruluşuna başvurun."
            });
        }
    }
}
