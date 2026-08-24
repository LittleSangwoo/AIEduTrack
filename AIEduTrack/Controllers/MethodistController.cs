using System.Text.Json;
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
            var allUsers = _dataRepository.GetAllUsers();

            // Отдаём реальных пользователей как JSON для динамического построения списка в JS
            ViewBag.AllUsersJson = JsonSerializer.Serialize(allUsers, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

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
        public IActionResult UploadData(IFormFile historyFile, IFormFile catalogFile, IFormFile? bookletFile)
        {
            if (historyFile == null || catalogFile == null)
                return BadRequest("Файлы истории и реестра курсов обязательны.");

            try
            {
                _dataRepository.ClearAll();

                using (var catalogStream = catalogFile.OpenReadStream())
                    _dataRepository.LoadCatalogFile(catalogStream, catalogFile.FileName);

                using (var historyStream = historyFile.OpenReadStream())
                    _dataRepository.LoadHistoryFile(historyStream, historyFile.FileName);

                if (bookletFile != null && bookletFile.Length > 0)
                {
                    using var bookletStream = bookletFile.OpenReadStream();
                    _dataRepository.LoadBookletFile(bookletStream);
                }

                var coursesCount = _dataRepository.GetAvailableCourses().Count;
                var usersCount = _dataRepository.GetAllUsers().Count;

                return Ok(new
                {
                    success = true,
                    message = $"Данные обновлены: {coursesCount} курсов в каталоге, {usersCount} сотрудников."
                });
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