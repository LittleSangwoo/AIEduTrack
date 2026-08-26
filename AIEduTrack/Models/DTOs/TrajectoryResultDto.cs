using System.Text.Json.Serialization;
namespace AIEduTrack.Models.DTOs
{
    public class TrajectoryResultDto
    {
        public string UserId { get; set; }
        public string UserRole { get; set; }
        public string Department { get; set; }
        public string ModelUsed { get; set; }
        public double ExecutionTimeMs { get; set; }

        // ДОБАВЛЯЕМ МЕТРИКИ ДЛЯ БЕНЧМАРКА:
        public int DraftStepsCount { get; set; } // Сколько шагов придумала LLM
        [JsonIgnore]
        public int HallucinationsFiltered { get; set; } // Сколько выдумано/не найдено в каталоге
        public int AlreadyPassedFiltered { get; set; } // Сколько отсеяно пройденных

        public string RawResponse { get; set; } = string.Empty;

        public List<TrajectoryStepDto> Steps { get; set; } = new();
    }
    public class TrajectoryStepDto
    {
        public int Order { get; set; } // Порядковый номер в маршруте
        public string CourseName { get; set; }
        public string CourseType { get; set; } // "ЭК" или "ППК"
        public string ShortDescription { get; set; } // Для всплывающего окна (тултипа)
        public string Justification { get; set; } // Обоснование назначения
        public List<string> TargetCompetencies { get; set; } // Развиваемые навыки
    }
}
