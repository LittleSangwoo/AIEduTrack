using AIEduTrack.Models;
using AIEduTrack.Models.DTOs;
using AIEduTrack.Services.LLM;

namespace AIEduTrack.Services.Agents
{
    public interface IExplainerAgent
    {
        Task<List<TrajectoryStepDto>> GenerateJustificationsAsync(List<TrajectoryStepDto> validTrajectory, UserProfile profile, ILLMClient llm);
    }
}
