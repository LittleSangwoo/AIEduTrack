using System;
using System.Collections.Generic;
using System.Linq;
using AIEduTrack.Models;
using AIEduTrack.Models.DTOs;

namespace AIEduTrack.Services.Agents
{
    public class ValidatorAgent : IValidatorAgent
    {
        public List<TrajectoryStepDto> Validate(List<TrajectoryStepDto> draft, UserProfile profile, List<Course> catalog)
        {
            var validSteps = new List<TrajectoryStepDto>();
            int currentOrder = 1;

            // Хешируем ID пройденных курсов для сверхбыстрого поиска (O(1))
            var passedCourseIds = profile.LearningHistory
                .Where(h => h.Status.Equals("Пройден", StringComparison.OrdinalIgnoreCase))
                .Select(h => h.CourseId)
                .ToHashSet();

            foreach (var step in draft)
            {
                // 1. КОНТУР ЗАЩИТЫ ОТ ГАЛЛЮЦИНАЦИЙ
                // Нейросеть могла слегка исказить название. Ищем реальный курс в БД.
                var realCourse = catalog.FirstOrDefault(c =>
                    c.Name.Contains(step.CourseName, StringComparison.OrdinalIgnoreCase) ||
                    step.CourseName.Contains(c.Name, StringComparison.OrdinalIgnoreCase));

                if (realCourse == null)
                {
                    // ИИ выдумал курс или сильно исказил название. Вырезаем.
                    continue;
                }

                // 2. КОНТУР ЗАЩИТЫ ОТ ПРОЙДЕННОГО МАТЕРИАЛА (!history.Any)
                if (passedCourseIds.Contains(realCourse.Id))
                {
                    // Пользователь уже успешно прошел этот курс. Вырезаем.
                    continue;
                }

                // 3. КОНТУР ЗАЩИТЫ ОТ ДУБЛИКАТОВ В САМОМ МАРШРУТЕ
                // Иногда LLM может порекомендовать один и тот же курс на 2-м и 4-м шаге.
                if (validSteps.Any(vs => vs.CourseName == realCourse.Name))
                {
                    continue;
                }

                // 4. СБОРКА ЧИСТОГО МАРШРУТА
                // Собираем DTO, подтягивая 100% достоверные данные напрямую из БД, 
                // а от нейросети берем только идею (компетенции).
                validSteps.Add(new TrajectoryStepDto
                {
                    Order = currentOrder++,
                    CourseName = realCourse.Name, // Берем точное, официальное название
                    CourseType = realCourse.Type, // Точный тип (ЭК/ППК)
                    ShortDescription = realCourse.Description, // Настоящее описание из реестра
                    TargetCompetencies = step.TargetCompetencies, // Оставляем навыки, которые выделил ИИ
                    Justification = step.Justification // Пока пустое, его заполнит Агент-Обоснователь
                });
            }

            return validSteps;
        }
    }
}