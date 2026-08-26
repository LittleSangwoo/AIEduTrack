using System.Text.Json;
using AIEduTrack.Models.DTOs;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System.Text.Encodings.Web;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AIEduTrack.Services.Report
{
    public interface ITrajectoryExportService
    {
        byte[] ExportToJson(TrajectoryResultDto trajectory);
        byte[] ExportToExcel(TrajectoryResultDto trajectory);
        byte[] ExportToWord(TrajectoryResultDto trajectory);
    }

    public class TrajectoryExportService : ITrajectoryExportService
    {
        public byte[] ExportToJson(TrajectoryResultDto trajectory)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // не экранировать кириллицу в \uXXXX
            };
            var json = JsonSerializer.Serialize(trajectory, options);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public byte[] ExportToExcel(TrajectoryResultDto trajectory)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Индивидуальная траектория");

            // Шапка с данными сотрудника
            ws.Cell(1, 1).Value = "ID сотрудника";
            ws.Cell(1, 2).Value = trajectory.UserId;
            ws.Cell(2, 1).Value = "Должность";
            ws.Cell(2, 2).Value = trajectory.UserRole;
            ws.Cell(3, 1).Value = "ИОГВ";
            ws.Cell(3, 2).Value = trajectory.Department;
            ws.Cell(4, 1).Value = "Сформировано с помощью";
            ws.Cell(4, 2).Value = trajectory.ModelUsed;

            ws.Range(1, 1, 4, 1).Style.Font.Bold = true;

            // Таблица шагов маршрута
            int headerRow = 6;
            string[] headers = { "№", "Название курса", "Тип", "Описание", "Компетенции", "Обоснование" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headers[i];
                ws.Cell(headerRow, i + 1).Style.Font.Bold = true;
                ws.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
                ws.Cell(headerRow, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int row = headerRow + 1;
            foreach (var step in trajectory.Steps)
            {
                ws.Cell(row, 1).Value = step.Order;
                ws.Cell(row, 2).Value = step.CourseName;
                ws.Cell(row, 3).Value = step.CourseType;
                ws.Cell(row, 4).Value = step.ShortDescription;
                ws.Cell(row, 5).Value = string.Join("; ", step.TargetCompetencies ?? new List<string>());
                ws.Cell(row, 6).Value = step.Justification;
                row++;
            }

            ws.Columns().AdjustToContents();
            var range = ws.Range(headerRow, 1, row - 1, headers.Length);
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportToWord(TrajectoryResultDto trajectory)
        {
            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                AddHeading(body, "Индивидуальная траектория обучения", 32, bold: true);

                // Профиль сотрудника
                AddParagraph(body, $"ID сотрудника: {trajectory.UserId}");
                AddParagraph(body, $"Должность: {trajectory.UserRole}");
                AddParagraph(body, $"ИОГВ: {trajectory.Department}");
                AddParagraph(body, $"Сформировано с помощью: {trajectory.ModelUsed}");
                AddParagraph(body, ""); // пустая строка-разделитель

                // Шаги маршрута
                foreach (var step in trajectory.Steps)
                {
                    AddHeading(body, $"{step.Order}. {step.CourseName} ({step.CourseType})", 26, bold: true);

                    if (!string.IsNullOrWhiteSpace(step.ShortDescription))
                        AddParagraph(body, step.ShortDescription);

                    if (step.TargetCompetencies != null && step.TargetCompetencies.Any())
                        AddParagraph(body, $"Развиваемые компетенции: {string.Join(", ", step.TargetCompetencies)}");

                    if (!string.IsNullOrWhiteSpace(step.Justification))
                        AddParagraph(body, $"Обоснование: {step.Justification}");

                    AddParagraph(body, ""); // разделитель между шагами
                }

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private static void AddHeading(Body body, string text, int fontSize, bool bold = false)
        {
            var run = new Run(new Text(text));
            var runProps = new RunProperties();
            if (bold) runProps.Append(new Bold());
            runProps.Append(new FontSize { Val = fontSize.ToString() });
            run.PrependChild(runProps);

            var paragraph = new Paragraph(run);
            body.Append(paragraph);
        }

        private static void AddParagraph(Body body, string text)
        {
            var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            var paragraph = new Paragraph(run);
            body.Append(paragraph);
        }
    }
}