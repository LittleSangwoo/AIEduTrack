using AIEduTrack.Models;
using AIEduTrack.Models.DTOs;

namespace AIEduTrack.Services.Agents
{
    public interface IValidatorAgent
    {
        List<TrajectoryStepDto> Validate(List<TrajectoryStepDto> draft, UserProfile profile, List<Course> catalog);
    }
}
