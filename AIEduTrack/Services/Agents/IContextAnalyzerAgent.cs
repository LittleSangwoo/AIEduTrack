using AIEduTrack.Models;

namespace AIEduTrack.Services.Agents
{
    public interface IContextAnalyzerAgent
    {
        // Добавили List<UserProfile> allUsers для анализа коллег
        Task<string> AnalyzeProfileAsync(UserProfile profile, List<Course> availableCourses, List<UserProfile> allUsers);
    }
}
