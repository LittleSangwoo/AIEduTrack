using AIEduTrack.Data;
using AIEduTrack.Models.DTOs;
using AIEduTrack.Services;
using AIEduTrack.Services.LLM;
using Microsoft.AspNetCore.Mvc;

namespace AIEduTrack.Controllers
{
    public class MethodistController : Controller
    {
        private readonly ILlmSettingsService _llmSettings;
        private readonly TrajectoryOrchestrator _orchestrator;
        private readonly IDataRepository _dataRepository;

        public MethodistController(ILlmSettingsService llmSettings, TrajectoryOrchestrator orchestrator, IDataRepository dataRepository)
        {
            _llmSettings = llmSettings;
            _orchestrator = orchestrator;
            _dataRepository = dataRepository;
        }

        public IActionResult Index()
        {
            var providers = _llmSettings.GetProviders().ToList();
            // Для методиста список сотрудников виден целиком — это служебный доступ, не наружу
            ViewBag.AllUsers = _dataRepository.GetAllUsers();
            return View(providers);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateTrajectory([FromBody] GenerateRequest request)
        {
            try
            {
                var result = await _orchestrator.GenerateAsync(request.UserId, request.LlmProvider);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UploadData(IFormFile historyFile, IFormFile catalogFile)
        {
            if (historyFile == null || catalogFile == null)
                return BadRequest("Оба файла должны быть загружены.");

            try
            {
                using var historyStream = historyFile.OpenReadStream();
                using var catalogStream = catalogFile.OpenReadStream();
                _dataRepository.UpdateData(historyStream, catalogStream);
                return Ok(new { success = true, message = "Данные обновлены (замена файлов выполнена)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка парсинга: {ex.Message}");
            }
        }
    }

    public class GenerateRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string LlmProvider { get; set; } = string.Empty;
    }
}