using Microsoft.AspNetCore.Mvc;
using AIEduTrack.Models;
using AIEduTrack.Services.LLM;

namespace AIEduTrack.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ILlmSettingsService _settingsService;

        public SettingsController(ILlmSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // Отдает саму страницу по адресу /Settings
        public IActionResult Index()
        {
            return View();
        }

        // --- API ДЛЯ JAVASCRIPT ---

        [HttpGet("api/settings/providers")]
        public IActionResult GetProviders()
        {
            return Ok(_settingsService.GetProviders());
        }

        [HttpPost("api/settings/providers")]
        public IActionResult SaveProvider([FromBody] LlmProviderConfig newProvider)
        {
            // Если создаем нового провайдера — генерируем ему уникальный ID
            if (string.IsNullOrEmpty(newProvider.Id))
            {
                newProvider.Id = Guid.NewGuid().ToString();
            }

            _settingsService.SaveProvider(newProvider);
            return Ok();
        }

        [HttpDelete("api/settings/providers/{id}")]
        public IActionResult DeleteProvider(string id)
        {
            _settingsService.DeleteProvider(id);
            return Ok();
        }
    }
}