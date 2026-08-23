using Microsoft.AspNetCore.Mvc;
using AIEduTrack.Services.Report;

namespace AIEduTrack.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IBenchmarkService _benchmarkService;

        public DashboardController(IBenchmarkService benchmarkService)
        {
            _benchmarkService = benchmarkService;
        }

        public IActionResult Analytics()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadBenchmarkReport()
        {
            try
            {
                // Запускаем прогон 20 профилей (может занять время)
                var fileBytes = await _benchmarkService.RunBenchmarkAndExportToExcelAsync(20);

                var fileName = $"AI_Benchmark_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(fileBytes, mimeType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка формирования отчета: {ex.Message}");
            }
        }
    }
}