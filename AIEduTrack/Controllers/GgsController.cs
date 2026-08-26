using AIEduTrack.Data;
using AIEduTrack.Models;
using AIEduTrack.Models.DTOs;
using AIEduTrack.Services;
using AIEduTrack.Services.LLM;
using AIEduTrack.Services.Report;
using Microsoft.AspNetCore.Mvc;

namespace AIEduTrack.Controllers
{
    public class GgsController : Controller
    {
        private readonly IDataRepository _dataRepository;
        private readonly TrajectoryOrchestrator _orchestrator;
        private readonly ILlmSettingsService _llmSettings;
        private readonly ITrajectoryExportService _exportService;

        public GgsController(
            IDataRepository dataRepository,
            TrajectoryOrchestrator orchestrator,
            ILlmSettingsService llmSettings,
            ITrajectoryExportService exportService)
        {
            _dataRepository = dataRepository;
            _orchestrator = orchestrator;
            _llmSettings = llmSettings;
            _exportService = exportService;
        }

        public IActionResult Index()
        {
            ViewBag.Providers = _llmSettings.GetProviders();

            // Уникальные должности и ведомства из уже загруженных данных —
            // чтобы новый ГГС выбирал из реальных значений, а не вводил текст вручную
            var allUsers = _dataRepository.GetAllUsers();
            ViewBag.KnownRoles = allUsers
                .Select(u => u.Role)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .OrderBy(r => r)
                .ToList();
            ViewBag.KnownDepartments = allUsers
                .Select(u => u.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return View();
        }

        [HttpPost]
        [HttpPost]
        public IActionResult FindMe([FromBody] IdRequest request)
        {
            var trimmedId = request.UserId?.Trim() ?? string.Empty;
            var profile = _dataRepository.GetAllUsers()
                .FirstOrDefault(u => u.Id.Trim().Equals(trimmedId, StringComparison.OrdinalIgnoreCase));

            if (profile == null)
                return Json(new { found = false });

            return Json(new { found = true, role = profile.Role, department = profile.Department });
        }

        [HttpPost]
        public async Task<IActionResult> GetMyTrajectory([FromBody] GgsRequest request)
        {
            try
            {
                var result = await _orchestrator.GenerateAsync(request.UserId!, request.LlmProvider);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetTrajectoryForNewGgs([FromBody] NewGgsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Role) || string.IsNullOrWhiteSpace(request.Department))
                    return Json(new { success = false, message = "Укажите должность и ИОГВ." });

                var tempProfile = new UserProfile
                {
                    Id = $"new-{Guid.NewGuid():N}",
                    Role = request.Role,
                    Department = request.Department,
                    LearningHistory = new List<LearningHistoryRecord>()
                };

                var result = await _orchestrator.GenerateAsync(tempProfile, request.LlmProvider);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Экспорт уже сгенерированной траектории (без повторного похода к LLM) —
        // фронт присылает обратно тот же TrajectoryResultDto, что получил ранее
        [HttpPost]
        public IActionResult ExportTrajectory([FromBody] TrajectoryResultDto trajectory, [FromQuery] string format)
        {
            try
            {
                byte[] fileBytes;
                string mimeType;
                string extension;

                switch (format?.ToLowerInvariant())
                {
                    case "excel":
                        fileBytes = _exportService.ExportToExcel(trajectory);
                        mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        extension = "xlsx";
                        break;
                    case "word":
                        fileBytes = _exportService.ExportToWord(trajectory);
                        mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        extension = "docx";
                        break;
                    case "json":
                    default:
                        fileBytes = _exportService.ExportToJson(trajectory);
                        mimeType = "application/json";
                        extension = "json";
                        break;
                }

                var fileName = $"Траектория_{trajectory.UserId}_{DateTime.Now:yyyyMMdd}.{extension}";
                return File(fileBytes, mimeType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка экспорта: {ex.Message}");
            }
        }
    }

    public class IdRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GgsRequest
    {
        public string? UserId { get; set; }
        public string LlmProvider { get; set; } = string.Empty;
    }

    public class NewGgsRequest
    {
        public string? Role { get; set; }
        public string? Department { get; set; }
        public string LlmProvider { get; set; } = string.Empty;
    }
}