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
