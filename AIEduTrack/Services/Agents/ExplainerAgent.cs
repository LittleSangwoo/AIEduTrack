using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIEduTrack.Models;
using AIEduTrack.Models.DTOs;
using AIEduTrack.Services.LLM;

namespace AIEduTrack.Services.Agents
{
    public class ExplainerAgent : IExplainerAgent
    {
        public async Task<List<TrajectoryStepDto>> GenerateJustificationsAsync(List<TrajectoryStepDto> validTrajectory, UserProfile profile, ILLMClient llm)
        {
            if (validTrajectory == null || !validTrajectory.Any())
                return validTrajectory ?? new List<TrajectoryStepDto>();

            // 1. Системный промпт: задаем роль наставника и жестко ограничиваем объем
            var systemPrompt = @"Ты — опытный HR-наставник Корпоративного университета. Твоя задача — коротко (1-2 предложения) и мотивирующе объяснить государственному гражданскому служащему (ГГС), почему ему назначен конкретный курс.
Правила:
1. Свяжи курс с должностью и ведомством (ИОГВ) ггс.
2. Упомяни навыки, которые он разовьет.
3. Пиши по делу, профессионально и дружелюбно.
4. Выведи ТОЛЬКО текст обоснования. Никаких кавычек, списков или вступительных фраз.";

            // 2. Проходим по каждому проверенному шагу и генерируем для него текст
            // Примечание: запускаем последовательно (foreach), а не параллельно (Task.WhenAll), 
            // чтобы не получить ошибку 429 (Too Many Requests) от бесплатных API вроде GigaChat или Groq.
            foreach (var step in validTrajectory)
            {
                var comps = step.TargetCompetencies != null && step.TargetCompetencies.Any()
                    ? string.Join(", ", step.TargetCompetencies)
                    : "Профессиональные компетенции";

                var userPrompt = $@"Должность: {profile.Role}
ИОГВ: {profile.Department}
Назначенный курс: {step.CourseName}
Развиваемые навыки: {comps}

Напиши обоснование для назначения.";

                try
                {
                    var responseText = await llm.GenerateResponseAsync(systemPrompt, userPrompt);

                    // 3. Косметическая очистка ответа (LLM иногда ставит лишние кавычки или переносы строк)
                    step.Justification = responseText
                        .Replace("\n", " ")
                        .Replace("\r", "")
                        .Replace("\"", "")
                        .Trim();
                }
                catch
                {
                    // Fallback на случай, если API отвалится на середине генерации
                    step.Justification = $"Курс рекомендован для развития следующих компетенций: {comps}.";
                }
            }

            return validTrajectory;
        }
    }
}