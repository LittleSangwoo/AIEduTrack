using System.Diagnostics;
using ClosedXML.Excel;
using AIEduTrack.Data;
using AIEduTrack.Services.LLM;

namespace AIEduTrack.Services.Report
{
    public interface IBenchmarkService
    {
        Task<byte[]> RunBenchmarkAndExportToExcelAsync(int profilesCount = 20);
    }

    public class BenchmarkService : IBenchmarkService
    {
        private readonly TrajectoryOrchestrator _orchestrator;
        private readonly IDataRepository _repository;
        private readonly ILlmSettingsService _settingsService;

        public BenchmarkService(TrajectoryOrchestrator orchestrator, IDataRepository repository, ILlmSettingsService settingsService)
        {
            _orchestrator = orchestrator;
            _repository = repository;
            _settingsService = settingsService;
        }

        public async Task<byte[]> RunBenchmarkAndExportToExcelAsync(int profilesCount = 20)
        {
            // Берем профили, у которых есть хоть какая-то история обучения для наглядности
            var testUsers = _repository.GetAllUsers()
                .Where(u => u.LearningHistory.Any())
                .Take(profilesCount)
                .ToList();

            var activeProviders = _settingsService.GetProviders().ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Анализ моделей (Бенчмарк)");

            // 1. Формируем заголовки
            string[] headers = {
                "ID Сотрудника", "Должность", "ИОГВ", "Провайдер", "Тип модели",
                "Время генерации (мс)", "JSON Валидность", "Выдано LLM",
                "Галлюцинации (отсеяно)", "Пройденные (отсеяно)", "Итого в маршруте"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int row = 2;

            // 2. Прогоняем каждый профиль через все модели
            foreach (var user in testUsers)
            {
                foreach (var provider in activeProviders)
                {
                    ws.Cell(row, 1).Value = user.Id;
                    ws.Cell(row, 2).Value = user.Role;
                    ws.Cell(row, 3).Value = user.Department;
                    ws.Cell(row, 4).Value = provider.Name;
                    ws.Cell(row, 5).Value = provider.AuthType;

                    try
                    {
                        // Запускаем пайплайн для конкретной модели
                        var result = await _orchestrator.GenerateAsync(user.Id, provider.Name);

                        ws.Cell(row, 6).Value = result.ExecutionTimeMs;
                        ws.Cell(row, 7).Value = "Да"; // Если парсинг прошел без исключений
                        ws.Cell(row, 8).Value = result.DraftStepsCount;
                        ws.Cell(row, 9).Value = result.HallucinationsFiltered;
                        ws.Cell(row, 10).Value = result.AlreadyPassedFiltered;
                        ws.Cell(row, 11).Value = result.Steps.Count;

                        // Подсветка ошибок/фильтраций для наглядности жюри
                        if (result.HallucinationsFiltered > 0)
                            ws.Cell(row, 9).Style.Font.FontColor = XLColor.Red;
                    }
                    catch (Exception ex)
                    {
                        ws.Cell(row, 6).Value = "-";
                        
                        // Записываем саму ошибку прямо в ячейку (вместо комментария)
                        ws.Cell(row, 7).Value = $"Ошибка: {ex.Message}";
                        ws.Cell(row, 7).Style.Font.FontColor = XLColor.Red; // Красим в красный
                        
                        ws.Cell(row, 8).Value = "-";
                        ws.Cell(row, 9).Value = "-";
                        ws.Cell(row, 10).Value = "-";
                        ws.Cell(row, 11).Value = "0";
                    }

                    row++;
                }
            }

            // 3. Косметическое форматирование
            ws.Columns().AdjustToContents();
            var range = ws.Range(1, 1, row - 1, headers.Length);
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            // 4. Возвращаем файл в виде массива байт
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}