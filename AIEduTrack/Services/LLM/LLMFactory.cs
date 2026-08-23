namespace AIEduTrack.Services.LLM
{
    public class LLMFactory : ILLMFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILlmSettingsService _settingsService;

        public LLMFactory(IHttpClientFactory httpClientFactory, ILlmSettingsService settingsService)
        {
            _httpClientFactory = httpClientFactory;
            _settingsService = settingsService;
        }

        public ILLMClient GetClient(string providerName)
        {
            // Ищем провайдера в json по имени (например, "ollama local1" или "gigachatApi")
            var config = _settingsService.GetProviders()
                .FirstOrDefault(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (config == null)
                throw new ArgumentException($"Провайдер с именем '{providerName}' не найден в настройках.");

            // Создаем HTTP клиент
            // Для GigaChat мы используем специальный клиент без проверки SSL-сертификатов Минцифры
            var httpClient = config.AuthType == "GigaChat"
                ? _httpClientFactory.CreateClient("GigaChatClient")
                : _httpClientFactory.CreateClient();

            // Возвращаем нужный класс в зависимости от типа авторизации
            if (config.AuthType == "OpenAI")
                return new OpenAiCompatibleClient(httpClient, config);

            if (config.AuthType == "GigaChat")
                return new GigaChatClient(httpClient, config);

            throw new ArgumentException($"Неизвестный AuthType: {config.AuthType}");
        }
    }
}