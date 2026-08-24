using ClosedXML.Excel;
using System.Text;

namespace AIEduTrack.Data
{
    public static class FileParsingHelper
    {
        // Универсальный вход: определяем формат по расширению и отдаем строки как Dictionary<Заголовок, Значение>
        public static List<Dictionary<string, string>> ParseRows(Stream stream, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            return ext switch
            {
                ".xlsx" or ".xls" => ParseExcel(stream),
                ".csv" => ParseCsv(stream),
                _ => throw new NotSupportedException($"Формат файла '{ext}' не поддерживается (ожидается .xlsx, .csv или .json).")
            };
        }

        private static List<Dictionary<string, string>> ParseExcel(Stream stream)
        {
            var result = new List<Dictionary<string, string>>();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var usedRange = worksheet.RangeUsed();
            if (usedRange == null) return result;

            var rows = usedRange.RowsUsed().ToList();
            if (rows.Count == 0) return result;

            // Первая строка — заголовки
            var headerRow = rows[0];
            var headers = headerRow.Cells()
                .Select(c => c.GetValue<string>().Trim())
                .ToList();

            foreach (var row in rows.Skip(1))
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i])) continue;
                    var cell = row.Cell(i + 1);
                    dict[headers[i]] = cell.GetValue<string>().Trim();
                }
                result.Add(dict);
            }

            return result;
        }

        private static List<Dictionary<string, string>> ParseCsv(Stream stream)
        {
            var result = new List<Dictionary<string, string>>();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }

            if (lines.Count == 0) return result;

            // Автоопределение разделителя: запятая или точка с запятой (частый случай для RU-выгрузок)
            char delimiter = lines[0].Count(c => c == ';') > lines[0].Count(c => c == ',') ? ';' : ',';

            var headers = SplitCsvLine(lines[0], delimiter);

            foreach (var dataLine in lines.Skip(1))
            {
                var values = SplitCsvLine(dataLine, delimiter);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < headers.Count && i < values.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i])) continue;
                    dict[headers[i]] = values[i].Trim();
                }
                result.Add(dict);
            }

            return result;
        }

        // Простой парсер CSV-строки с поддержкой кавычек ("значение, с запятой")
        private static List<string> SplitCsvLine(string line, char delimiter)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    values.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            values.Add(current.ToString().Trim());
            return values;
        }

        // Поиск значения по списку возможных названий колонки (частичное совпадение, регистронезависимо)
        public static string? FindValue(Dictionary<string, string> row, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                var match = row.Keys.FirstOrDefault(k => k.Contains(alias, StringComparison.OrdinalIgnoreCase));
                if (match != null) return row[match];
            }
            return null;
        }
    }
}