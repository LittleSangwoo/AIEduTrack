using System.Text.RegularExpressions;
using AIEduTrack.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AIEduTrack.Data
{
    public static class PdfBookletParser
    {
        private static void Log(string message)
        {
            Console.WriteLine(message);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public static List<Course> ParseCourses(Stream pdfStream)
        {
            var courses = new List<Course>();

            using var document = PdfDocument.Open(pdfStream);
            var allLines = new List<string>();
            int pageCount = 0;

            foreach (var page in document.GetPages())
            {
                allLines.AddRange(ReconstructLines(page));
                pageCount++;
            }

            Log($"[PDF DIAGNOSTIC] Страниц обработано: {pageCount}");
            Log($"[PDF DIAGNOSTIC] Восстановлено строк по координатам: {allLines.Count}");
            if (allLines.Count > 0)
            {
                Log("[PDF DIAGNOSTIC] Первые 15 строк:");
                foreach (var l in allLines.Take(15))
                    Log($"    -> {l.Substring(0, Math.Min(90, l.Length))}");
            }

            int titleLinesFound = allLines.Count(IsTitleLine);
            int categoryLinesFound = allLines.Count(IsCategoryLine);
            Log($"[PDF DIAGNOSTIC] Строк-ЗАГОЛОВКОВ: {titleLinesFound}, строк-КАТЕГОРИЙ: {categoryLinesFound}");

            int i = 0;
            while (i < allLines.Count - 1)
            {
                if (IsTitleLine(allLines[i]))
                {
                    var titleParts = new List<string> { allLines[i] };
                    int j = i + 1;

                    while (j < allLines.Count && !string.IsNullOrWhiteSpace(allLines[j])
                           && IsTitleLine(allLines[j]) && !IsCategoryLine(allLines[j])
                           && titleParts.Count < 3)
                    {
                        titleParts.Add(allLines[j]);
                        j++;
                    }

                    if (j < allLines.Count && IsCategoryLine(allLines[j]))
                    {
                        var fullTitle = string.Join(" ", titleParts).Trim();
                        var category = allLines[j];

                        var descLines = new List<string>();
                        int k = j + 1;
                        while (k < allLines.Count
                               && !allLines[k].Contains("КЛЮЧЕВЫЕ ТЕМЫ", StringComparison.OrdinalIgnoreCase)
                               && k < j + 15)
                        {
                            if (!string.IsNullOrWhiteSpace(allLines[k]))
                                descLines.Add(allLines[k]);
                            k++;
                        }

                        if (!courses.Any(c => c.Name.Equals(fullTitle, StringComparison.OrdinalIgnoreCase)))
                        {
                            courses.Add(new Course
                            {
                                Id = fullTitle,
                                Name = fullTitle,
                                Type = "ППК",
                                Description = string.Join(" ", descLines)
                            });
                        }

                        i = k;
                        continue;
                    }
                }
                i++;
            }

            Log($"[PDF DIAGNOSTIC] ИТОГО извлечено карточек курсов: {courses.Count}");

            return courses;
        }

        // Собираем визуальные строки страницы по координатам слов:
        // группируем слова с близким Top (одна строка), сортируем внутри группы по Left (слева направо)
        private static List<string> ReconstructLines(Page page)
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) return new List<string>();

            const double yTolerance = 3.0; // допуск группировки по вертикали, в points

            // Сортируем сверху вниз (Top у PdfPig больше = выше на странице)
            var sorted = words.OrderByDescending(w => w.BoundingBox.Top).ToList();

            var lines = new List<List<Word>>();
            foreach (var word in sorted)
            {
                var line = lines.LastOrDefault();
                if (line != null && Math.Abs(line[0].BoundingBox.Top - word.BoundingBox.Top) <= yTolerance)
                {
                    line.Add(word);
                }
                else
                {
                    lines.Add(new List<Word> { word });
                }
            }

            var result = new List<string>();
            foreach (var line in lines)
            {
                var orderedWords = line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text);
                result.Add(string.Join(" ", orderedWords).Trim());
            }

            return result;
        }

        private static bool IsTitleLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length <= 8) return false;
            if (line.TrimStart().StartsWith("•")) return false;

            var letters = line.Where(char.IsLetter).ToList();
            if (letters.Count == 0) return false;

            var upperRatio = letters.Count(char.IsUpper) / (double)letters.Count;
            return upperRatio > 0.8;
        }

        private static readonly Regex NumberPrefix = new(@"^\s*\d", RegexOptions.Compiled);

        private static bool IsCategoryLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (!line.Contains('/')) return false;
            if (line.TrimStart().StartsWith("•")) return false;
            if (line.Length > 100) return false;
            if (line.Contains("актуально", StringComparison.OrdinalIgnoreCase)) return false;
            if (NumberPrefix.IsMatch(line)) return false;
            return true;
        }
    }
}