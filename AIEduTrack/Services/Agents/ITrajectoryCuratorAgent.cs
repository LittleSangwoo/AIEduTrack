using AIEduTrack.Models;
using AIEduTrack.Models.DTOs;
using AIEduTrack.Services.LLM;

namespace AIEduTrack.Services.Agents
{
    public interface ITrajectoryCuratorAgent
    {
        // Добавили List<Course> catalogContext
        Task<List<TrajectoryStepDto>> DraftTrajectoryAsync(string context, ILLMClient llm, List<Course> catalogContext);
    }
}
