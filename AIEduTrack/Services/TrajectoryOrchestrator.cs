using AIEduTrack.Data;
using AIEduTrack.Models.DTOs;
using AIEduTrack.Services.Agents;
using AIEduTrack.Services.LLM;

namespace AIEduTrack.Services
{
    public class TrajectoryOrchestrator
    {
        private readonly ILLMFactory _llmFactory;
        private readonly IContextAnalyzerAgent _analyzer;
        private readonly ITrajectoryCuratorAgent _curator;
        private readonly IValidatorAgent _validator;
        private readonly IExplainerAgent _explainer;
        private readonly IMockDataRepository _repository;

        public TrajectoryOrchestrator(
            ILLMFactory llmFactory,
            IContextAnalyzerAgent analyzer,
            ITrajectoryCuratorAgent curator,
            IValidatorAgent validator,
            IExplainerAgent explainer,
            IMockDataRepository repository)
        {
            _llmFactory = llmFactory;
            _analyzer = analyzer;
            _curator = curator;
            _validator = validator;
            _explainer = explainer;
            _repository = repository;
        }

        public async Task<TrajectoryResultDto> GenerateAsync(string userId, string providerType)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var llm = _llmFactory.GetClient(providerType);
            var profile = _repository.GetUserProfile(userId);
            var catalog = _repository.GetCatalog();

            var context = await _analyzer.AnalyzeProfileAsync(profile, catalog);
            var draft = await _curator.DraftTrajectoryAsync(context, llm);
            var validSteps = _validator.Validate(draft, profile, catalog);
            var finalSteps = await _explainer.GenerateJustificationsAsync(validSteps, profile, llm);

            watch.Stop();

            return new TrajectoryResultDto
            {
                UserId = profile.Id,
                UserRole = profile.Role,
                Department = profile.Department,
                ModelUsed = llm.ProviderName,
                ExecutionTimeMs = watch.ElapsedMilliseconds,
                Steps = finalSteps
            };
        }
    }
}
