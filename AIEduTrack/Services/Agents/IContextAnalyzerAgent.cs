using AIEduTrack.Models;

namespace AIEduTrack.Services.Agents
{
    public interface IContextAnalyzerAgent
    {
        Task<string> AnalyzeProfileAsync(UserProfile profile, List<Course> availableCourses);
    }
}
