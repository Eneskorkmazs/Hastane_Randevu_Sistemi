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
        [IgnoreAntiforgeryToken]
        public IActionResult Analyze(SymptomViewModel model)
        {
            var selectedSymptoms = model.SelectedSymptoms ?? new List<string>();
            if (!selectedSymptoms.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedSymptoms), "Lutfen en az bir semptom seciniz.");
                return View("Index", model);
            }

            var selectedLabels = SymptomViewModel.AvailableSymptoms
                .Where(s => selectedSymptoms.Contains(s.Key))
                .Select(s => s.Label)
                .ToList();

            var result = new SymptomResultViewModel
            {
                SelectedSymptomLabels = selectedLabels,
                Suggestions = _symptomCheckerService.Analyze(selectedSymptoms)
            };

            if (!result.Suggestions.Any())
            {
                result.WarningMessage = "Sectiginiz semptomlara gore net bir bolum onerisi olusturulamadi.";
            }

            return View("Result", result);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Chat([FromBody] ChatMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Mesaj boş olamaz.");
            }

            var response = _symptomCheckerService.ProcessChat(request.Message, request.History ?? new List<string>());
            return Json(response);
        }
    }
}
