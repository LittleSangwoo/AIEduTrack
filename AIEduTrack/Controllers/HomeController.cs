using AIEduTrack.Data;
using AIEduTrack.Models.DTOs;
using AIEduTrack.Services; // Пространство имен твоего TrajectoryOrchestrator
using AIEduTrack.Services.LLM;
using Microsoft.AspNetCore.Mvc;

namespace AIEduTrack.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILlmSettingsService _llmSettings;
        private readonly TrajectoryOrchestrator _orchestrator;
        private readonly IDataRepository _dataRepository; 

        public HomeController(ILlmSettingsService llmSettings, TrajectoryOrchestrator orchestrator, IDataRepository dataRepository)
        {
            _llmSettings = llmSettings;
            _orchestrator = orchestrator; 
            _dataRepository = dataRepository;
        }

        public IActionResult Index()
        {
            var providers = _llmSettings.GetProviders().ToList();
            return View(providers);
        }

        // ТОЧКА ВХОДА ДЛЯ НЕЙРОСЕТЕЙ
        [HttpPost]
        public async Task<IActionResult> GenerateTrajectory([FromBody] GenerateRequest request)
        {
            try
            {
                // Запускаем реальный конвейер ИИ-агентов
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

                // Передаем потоки файлов в наш один репозиторий
                _dataRepository.UpdateData(historyStream, catalogStream);

                return Ok(new { success = true, message = "База данных успешно обновлена!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка парсинга: {ex.Message}");
            }
        }
    }



    // Класс для приема данных из UI
    public class GenerateRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string LlmProvider { get; set; } = string.Empty;
    }
}