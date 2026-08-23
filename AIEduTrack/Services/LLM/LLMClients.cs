using System.Threading.Tasks;

namespace AIEduTrack.Services.LLM
{
    public class GigaChatClient : ILLMClient
    {
        public string ProviderName => "GigaChat (Russian)";

        public async Task<string> GenerateResponseAsync(string systemContext, string userPrompt)
        {
            // Здесь будет HTTP-запрос к API Сбера
            return await Task.FromResult("Заглушка от GigaChat");
        }
    }

    public class GroqClient : ILLMClient
    {
        public string ProviderName => "Groq (Foreign)";

        public async Task<string> GenerateResponseAsync(string systemContext, string userPrompt)
        {
            // Здесь будет HTTP-запрос к API Groq
            return await Task.FromResult("Заглушка от Groq");
        }
    }

    public class OllamaClient : ILLMClient
    {
        public string ProviderName => "Ollama (Local)";

        public async Task<string> GenerateResponseAsync(string systemContext, string userPrompt)
        {
            // Здесь будет HTTP-запрос к локальному localhost:11434
            return await Task.FromResult("Заглушка от Ollama");
        }
    }
}