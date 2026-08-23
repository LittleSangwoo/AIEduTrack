using ClosedXML.Excel;
using AIEduTrack.Models;

namespace AIEduTrack.Data
{
    public class ExcelDataRepository : IDataRepository
    {
        // Здесь хранятся данные в оперативной памяти 
        private List<Course> _courses = new();
        private List<UserProfile> _users = new();

        // Главный метод, который принимает два файла (потока) одновременно
        public void UpdateData(Stream historyStream, Stream catalogStream)
        {
            _courses.Clear();
            _users.Clear();

            // Вызываем два разных метода-парсера
            ParseCatalog(catalogStream);
            ParseHistory(historyStream);
        }

        private void ParseCatalog(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1); // Первая вкладка Excel
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Пропуск заголовков

            foreach (var row in rows)
            {
                var courseName = row.Cell(1).GetValue<string>().Trim(); // 1 колонка: Название
                if (string.IsNullOrWhiteSpace(courseName)) continue;

                _courses.Add(new Course
                {
                    Id = courseName,
                    Name = courseName,
                    Type = "ЭК/ППК", // В файле реестра нет колонки "Тип"
                    // Склеиваем Аннотацию (кол. 2) и Результаты (кол. 4)
                    Description = row.Cell(2).GetValue<string>() + " " + row.Cell(4).GetValue<string>()
                });
            }
        }

        private void ParseHistory(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            var userDict = new Dictionary<string, UserProfile>();

            foreach (var row in rows)
            {
                var userName = row.Cell(1).GetValue<string>().Trim(); // 1 колонка: ФИО
                if (string.IsNullOrWhiteSpace(userName)) continue;

                // Если пользователя еще нет, создаем его
                if (!userDict.ContainsKey(userName))
                {
                    userDict[userName] = new UserProfile
                    {
                        Id = userName, // Используем ФИО как ID для простоты
                        Role = row.Cell(2).GetValue<string>(),       // 2 колонка: Должность
                        Department = row.Cell(3).GetValue<string>(), // 3 колонка: ИОГВ
                        LearningHistory = new List<LearningHistoryRecord>()
                    };
                }

                // Добавляем курс в историю
                userDict[userName].LearningHistory.Add(new LearningHistoryRecord
                {
                    CourseId = row.Cell(5).GetValue<string>().Trim(),
                    CourseName = row.Cell(5).GetValue<string>(), // 5 колонка: Название курса
                    Status = row.Cell(6).GetValue<string>()      // 6 колонка: Статус
                });
            }

            _users = userDict.Values.ToList();
        }

        public List<Course> GetAvailableCourses() => _courses;
        public List<UserProfile> GetAllUsers() => _users;
        public UserProfile GetProfile(string userId) => _users.FirstOrDefault(u => u.Id == userId) ?? new UserProfile();
    }
}