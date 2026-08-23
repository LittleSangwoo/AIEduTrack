namespace AIEduTrack.Models
{
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Должность
        public string Department { get; set; } = string.Empty; // ИОГВ
        // Добавили историю обучения
        public List<LearningHistoryRecord> LearningHistory { get; set; } = new();
    }
    // Добавили класс записи об обучении
    public class LearningHistoryRecord
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Пройден", "Не пройден", "В процессе"
    }

    public class Course
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // ЭК или ППК
        public string Description { get; set; } = string.Empty;
    }
}