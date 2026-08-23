namespace AIEduTrack.Models
{
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Должность
        public string Department { get; set; } = string.Empty; // ИОГВ
    }

    public class Course
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // ЭК или ППК
        public string Description { get; set; } = string.Empty;
    }
}