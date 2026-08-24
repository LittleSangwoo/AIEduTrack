using AIEduTrack.Data;
using AIEduTrack.Models;
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
        private readonly IDataRepository _repository;

        public TrajectoryOrchestrator(
            ILLMFactory llmFactory,
            IContextAnalyzerAgent analyzer,
            ITrajectoryCuratorAgent curator,
            IValidatorAgent validator,
            IExplainerAgent explainer,
            IDataRepository repository)
        {
            _llmFactory = llmFactory;
            _analyzer = analyzer;
            _curator = curator;
            _validator = validator;
            _explainer = explainer;
            _repository = repository;
        }

        // Старый вход: ищем существующего пользователя по ID (сценарий методиста / сотрудника с историей)
        public async Task<TrajectoryResultDto> GenerateAsync(string userId, string providerType)
        {
            var profile = _repository.GetProfile(userId);
            return await GenerateAsync(profile, providerType);
        }

        // Новый вход: профиль уже готов (в т.ч. "новый сотрудник" — Role/Department без истории)
        public async Task<TrajectoryResultDto> GenerateAsync(UserProfile profile, string providerType)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var llm = _llmFactory.GetClient(providerType);
            var catalog = _repository.GetAvailableCourses();
            var allUsers = _repository.GetAllUsers();

            if (catalog == null || catalog.Count == 0)
            {
                throw new Exception("Каталог курсов пуст. Данные ещё не загружены методистом — попробуйте позже или обратитесь к администратору системы.");
            }

            // 1. Сбор контекста (для нового юзера LearningHistory пустая — агент это учитывает)
            var context = await _analyzer.AnalyzeProfileAsync(profile, catalog, allUsers);

            // 2. Генерация
            var draft = await _curator.DraftTrajectoryAsync(context, llm, catalog);
            Console.WriteLine($"\n[АГЕНТ-МЕТОДИСТ] Предложил курсов: {draft.Count}");

            // 3. Валидация (защита от пройденного и галлюцинаций)
            var validSteps = _validator.Validate(draft, profile, catalog);
            Console.WriteLine($"[АГЕНТ-ВАЛИДАТОР] Оставил после фильтрации: {validSteps.Count}");

            // 4. Обоснование
            var finalSteps = await _explainer.GenerateJustificationsAsync(validSteps, profile, llm);

            watch.Stop();

            return new TrajectoryResultDto
            {
                UserId = profile.Id,
                UserRole = profile.Role,
                Department = profile.Department,
                ModelUsed = llm.ProviderName,
                ExecutionTimeMs = watch.ElapsedMilliseconds,
                DraftStepsCount = draft.Count,
                AlreadyPassedFiltered = draft.Count - validSteps.Count,
                Steps = finalSteps
            };
        }
    }
}