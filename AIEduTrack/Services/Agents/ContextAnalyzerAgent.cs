using AIEduTrack.Models;
using System.Text;

namespace AIEduTrack.Services.Agents
{
    public class ContextAnalyzerAgent : IContextAnalyzerAgent
    {
        public Task<string> AnalyzeProfileAsync(UserProfile profile, List<Course> availableCourses, List<UserProfile> allUsers)
        {
            // 1. Собираем личную историю
            var passedCourses = profile.LearningHistory
                .Where(h => h.Status.Equals("Пройден", StringComparison.OrdinalIgnoreCase))
                .Select(h => h.CourseName)
                .ToList();

            var failedCourses = profile.LearningHistory
                .Where(h => h.Status.Equals("Не пройден", StringComparison.OrdinalIgnoreCase))
                .Select(h => h.CourseName)
                .ToList();

            // 2. АНАЛИЗ КОЛЛЕГ (Поиск паттернов по ИОГВ и Должности)
            var colleagues = allUsers.Where(u =>
                u.Role == profile.Role &&
                u.Department == profile.Department &&
                u.Id != profile.Id).ToList();

            // Вычисляем Топ-5 самых популярных успешно пройденных курсов среди коллег
            var popularAmongColleagues = colleagues
                .SelectMany(u => u.LearningHistory)
                .Where(h => h.Status.Equals("Пройден", StringComparison.OrdinalIgnoreCase))
                .GroupBy(h => h.CourseName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            // 3. Формируем контекст для LLM
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("[ПРОФИЛЬ ГГС]");
            contextBuilder.AppendLine($"Должность: {profile.Role}");
            contextBuilder.AppendLine($"ИОГВ (Ведомство): {profile.Department}");

            contextBuilder.AppendLine("\n[ЛИЧНАЯ ИСТОРИЯ ОБУЧЕНИЯ]");
            contextBuilder.AppendLine($"Успешно пройдено: {(passedCourses.Any() ? string.Join("; ", passedCourses) : "Нет данных")}");
            contextBuilder.AppendLine($"Не пройдено (дефицит навыков): {(failedCourses.Any() ? string.Join("; ", failedCourses) : "Нет")}");

            contextBuilder.AppendLine("\n[ПАТТЕРНЫ КОЛЛЕГ]");
            contextBuilder.AppendLine("Успешный опыт государственных гражданских служащих с аналогичной должностью в этом же ИОГВ:");
            contextBuilder.AppendLine(popularAmongColleagues.Any()
                ? string.Join("; ", popularAmongColleagues)
                : "Недостаточно данных для анализа коллег.");

            return Task.FromResult(contextBuilder.ToString());
        }
    }
}