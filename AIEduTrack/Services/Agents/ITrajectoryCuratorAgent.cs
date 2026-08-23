using AIEduTrack.Models.DTOs;
using AIEduTrack.Services.LLM;

namespace AIEduTrack.Services.Agents
{
    public interface ITrajectoryCuratorAgent
    {
        Task<List<TrajectoryStepDto>> DraftTrajectoryAsync(string context, ILLMClient llm);
    }
}
