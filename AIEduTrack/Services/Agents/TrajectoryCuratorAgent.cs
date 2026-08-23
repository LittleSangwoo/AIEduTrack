using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AIEduTrack.Models;
using AIEduTrack.Models.DTOs;
using AIEduTrack.Services.LLM;

namespace AIEduTrack.Services.Agents
{
    public class TrajectoryCuratorAgent : ITrajectoryCuratorAgent
    {
        public async Task<List<TrajectoryStepDto>> DraftTrajectoryAsync(string context, ILLMClient llm, List<Course> catalogContext)
        {
            // 1. Системный промпт: задаем роль и жесткие рамки формата
            var systemPrompt = @"Ты — эксперт-методист Корпоративного университета. Твоя задача — составить логичную образовательную траекторию для государственного гражданского служащего (ГГС).
Правила:
1. Выбирай курсы ТОЛЬКО из предложенного каталога.
2. Траектория должна состоять из 3-5 курсов, выстроенных от простых к сложным.
3. Учитывай должность и ИОГВ сотрудника.
4. ВЫВЕДИ ОТВЕТ СТРОГО В ФОРМАТЕ JSON МАССИВА. Никаких вступительных слов, никакого текста до или после JSON. Не используй markdown-разметку (```json).

Требуемая схема JSON:
[
  {
    ""CourseName"": ""Точное название курса из каталога"",
    ""TargetCompetencies"": [""Навык 1"", ""Навык 2""]
  }
]";

            // 2. Ужимаем каталог, чтобы сэкономить токены и деньги/память
            // Передаем только ID, Название и Тип, без длинных описаний
            var slimCatalog = string.Join("\n", catalogContext.Select(c => $"- {c.Name} ({c.Type})"));

            // 3. Формируем пользовательский промпт
            var userPrompt = $@"{context}

[КАТАЛОГ ДОСТУПНЫХ КУРСОВ]:
{slimCatalog}

Сгенерируй JSON-массив с рекомендациями.";

            // 4. Отправляем запрос в LLM
            var responseText = await llm.GenerateResponseAsync(systemPrompt, userPrompt);

            // 5. Безопасный парсинг ответа
            return ParseJsonSafely(responseText);
        }

        private List<TrajectoryStepDto> ParseJsonSafely(string responseText)
        {
            // Выводим сырой ответ в консоль Visual Studio, чтобы видеть, что придумал ИИ
            Console.WriteLine($"\n=== СЫРОЙ ОТВЕТ LLM ===\n{responseText}\n=======================\n");
            System.Diagnostics.Debug.WriteLine($"\n=== СЫРОЙ ОТВЕТ LLM ===\n{responseText}\n=======================\n");

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                // Ищем четкие границы массива
                int startIndex = responseText.IndexOf('[');
                int endIndex = responseText.LastIndexOf(']');

                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    // Вырезаем ровно то, что находится между скобками [ ]
                    var cleanJson = responseText.Substring(startIndex, endIndex - startIndex + 1);
                    return JsonSerializer.Deserialize<List<TrajectoryStepDto>>(cleanJson, options) ?? new List<TrajectoryStepDto>();
                }

                Console.WriteLine("JSON массив не найден в ответе.");
                return new List<TrajectoryStepDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга JSON: {ex.Message}");
                return new List<TrajectoryStepDto>();
            }
        }
    }
}