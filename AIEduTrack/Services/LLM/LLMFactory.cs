namespace AIEduTrack.Services.LLM
{
    public class LLMFactory : ILLMFactory
    {
        private readonly IEnumerable<ILLMClient> _clients;

        public LLMFactory(IEnumerable<ILLMClient> clients)
        {
            _clients = clients;
        }

        public ILLMClient GetClient(string providerType)
        {
            return providerType.ToLower() switch
            {
                "russian" => _clients.First(c => c is GigaChatClient),
                "foreign" => _clients.First(c => c is GroqClient),
                "local" => _clients.First(c => c is OllamaClient),
                _ => throw new ArgumentException("Неизвестный провайдер")
            };
        }
    }
}
