using System.Text.Json;
using AIEduTrack.Models;

namespace AIEduTrack.Data
{
    public class ExcelDataRepository : IDataRepository
    {
        private List<Course> _courses = new();
        private List<UserProfile> _users = new();
        private readonly Dictionary<string, UserProfile> _userIndex = new(StringComparer.OrdinalIgnoreCase);

        public void ClearAll()
        {
            _courses.Clear();
            _users.Clear();
            _userIndex.Clear();
        }

        public void UpdateData(Stream historyStream, Stream catalogStream)
        {
            ClearAll();
            LoadCatalogFile(catalogStream, "catalog.xlsx");
            LoadHistoryFile(historyStream, "history.xlsx");
        }

        public void LoadCatalogFile(Stream stream, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (ext == ".json")
            {
                LoadCatalogJson(stream);
                return;
            }

            // Реальная структура "Реестр_электронных_курсов.xlsx": лист "Отчет",
            // колонки: 1-Название курса, 2-Аннотация, 3-Цель/задачи, 4-Результаты
            var rows = FileParsingHelper.ParseRows(stream, fileName);

            foreach (var row in rows)
            {
                var name = FileParsingHelper.FindValue(row, "название", "наименование");
                if (string.IsNullOrWhiteSpace(name)) continue;

                var descriptionParts = new[] { "аннотация", "цель", "результат" }
                    .Select(alias => FileParsingHelper.FindValue(row, alias))
                    .Where(v => !string.IsNullOrWhiteSpace(v));

                var description = string.Join(" ", descriptionParts);

                if (_courses.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;

                _courses.Add(new Course
                {
                    Id = name,
                    Name = name,
                    Type = "ЭК", // реестр содержит только электронные курсы
                    Description = string.IsNullOrWhiteSpace(description) ? "Описание отсутствует" : description
                });
            }
        }

        // Буклет "Линейка образовательных программ" — источник карточек ППК (программ повышения квалификации),
        // которых нет в реестре электронных курсов. Без него ~60% истории обучения не находит соответствия в каталоге.
        public void LoadBookletFile(Stream pdfStream)
        {
            var bookletCourses = PdfBookletParser.ParseCourses(pdfStream);

            foreach (var course in bookletCourses)
            {
                if (_courses.Any(c => c.Name.Equals(course.Name, StringComparison.OrdinalIgnoreCase))) continue;
                _courses.Add(course);
            }
        }

        public void LoadHistoryFile(Stream stream, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (ext == ".json")
            {
                LoadHistoryJson(stream);
                return;
            }

            // Реальная структура "выгрузка_с_историей_обучения.xlsx": лист "ГГС 2024-2025",
            // колонки: 1-ФИО, 2-Должность, 3-ИОГВ, 4-Тип(ЭК/ППК, без заголовка), 5-Курс, 6-Статус
            var rows = FileParsingHelper.ParseRows(stream, fileName);

            foreach (var row in rows)
            {
                var userName = FileParsingHelper.FindValue(row, "фио");
                if (string.IsNullOrWhiteSpace(userName)) continue;

                if (!_userIndex.TryGetValue(userName, out var profile))
                {
                    profile = new UserProfile
                    {
                        Id = userName,
                        Role = FileParsingHelper.FindValue(row, "должность") ?? "",
                        Department = FileParsingHelper.FindValue(row, "иогв") ?? "",
                        LearningHistory = new List<LearningHistoryRecord>()
                    };
                    _userIndex[userName] = profile;
                    _users.Add(profile);
                }

                var courseName = FileParsingHelper.FindValue(row, "курс");
                if (string.IsNullOrWhiteSpace(courseName)) continue;

                var status = FileParsingHelper.FindValue(row, "статус") ?? "Не пройден";

                profile.LearningHistory.Add(new LearningHistoryRecord
                {
                    CourseId = courseName,
                    CourseName = courseName,
                    Status = status
                });
            }
        }

        private void LoadCatalogJson(Stream stream)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var courses = JsonSerializer.Deserialize<List<Course>>(stream, options) ?? new();

            foreach (var c in courses)
            {
                if (!_courses.Any(x => x.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase)))
                    _courses.Add(c);
            }
        }

        private void LoadHistoryJson(Stream stream)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var users = JsonSerializer.Deserialize<List<UserProfile>>(stream, options) ?? new();

            foreach (var u in users)
            {
                if (!_userIndex.ContainsKey(u.Id))
                {
                    _userIndex[u.Id] = u;
                    _users.Add(u);
                }
            }
        }

        public void LoadFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Директория не найдена: {directoryPath}");

            var files = Directory.GetFiles(directoryPath)
                .Where(f => new[] { ".xlsx", ".xls", ".csv", ".json", ".pdf" }
                    .Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var lower = fileName.ToLowerInvariant();
                var ext = Path.GetExtension(fileName).ToLowerInvariant();

                bool isBooklet = ext == ".pdf" || lower.Contains("буклет") || lower.Contains("линейка");
                bool isCatalog = lower.Contains("реестр") || lower.Contains("каталог") || lower.Contains("catalog");
                bool isHistory = lower.Contains("истори") || lower.Contains("обучен") || lower.Contains("history");

                using var fs = File.OpenRead(filePath);

                try
                {
                    if (isBooklet)
                        LoadBookletFile(fs);
                    else if (isCatalog && !isHistory)
                        LoadCatalogFile(fs, fileName);
                    else if (isHistory)
                        LoadHistoryFile(fs, fileName);
                    else
                        Console.WriteLine($"[LoadFromDirectory] Файл '{fileName}' не удалось классифицировать. Пропущен.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LoadFromDirectory] Ошибка при загрузке '{fileName}': {ex.Message}");
                }
            }
        }

        public List<Course> GetAvailableCourses() => _courses;
        public List<UserProfile> GetAllUsers() => _users;
        public UserProfile GetProfile(string userId)
        {
            var trimmedId = userId?.Trim() ?? string.Empty;
            return _users.FirstOrDefault(u => u.Id.Trim().Equals(trimmedId, StringComparison.OrdinalIgnoreCase))
                ?? new UserProfile();
        }
    }
    }