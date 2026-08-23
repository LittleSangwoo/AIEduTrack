using System.Text.Json;
using AIEduTrack.Models;

namespace AIEduTrack.Services.LLM
{
    public interface ILlmSettingsService
    {
        List<LlmProviderConfig> GetProviders();
        LlmProviderConfig? GetProviderById(string id);
        void SaveProvider(LlmProviderConfig provider);
        void DeleteProvider(string id);
    }

    public class LlmSettingsService : ILlmSettingsService
    {
        private readonly string _filePath;
        private static readonly object _lock = new();

        public LlmSettingsService(IWebHostEnvironment env)
        {
            _filePath = Path.Combine(env.ContentRootPath, "llm_providers.json");
            EnsureFileExists();
        }

        private void EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                var defaultProviders = new List<LlmProviderConfig>
                {
                    new() {
                        Id = Guid.NewGuid().ToString(),
                        Name = "gigachatApi",
                        AuthType = "GigaChat",
                        BaseUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions",
                        ApiKey = "-",
                        Model = "GigaChat",
                        Scope = "5534ed3b-1d6b-4609-acb3-cf262c78e15f",
                        IsLocal = false
                    },
                    new() {
                        Id = Guid.NewGuid().ToString(),
                        Name = "ollama local1",
                        AuthType = "OpenAI",
                        BaseUrl = "http://localhost:11434/v1/chat/completions",
                        ApiKey = "",
                        Model = "llama3",
                        IsLocal = true
                    }
                };
                File.WriteAllText(_filePath, JsonSerializer.Serialize(defaultProviders, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public List<LlmProviderConfig> GetProviders()
        {
            lock (_lock)
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<LlmProviderConfig>>(json) ?? new List<LlmProviderConfig>();
            }
        }

        public LlmProviderConfig? GetProviderById(string id) => GetProviders().FirstOrDefault(p => p.Id == id);

        public void SaveProvider(LlmProviderConfig provider)
        {
            lock (_lock)
            {
                var providers = GetProviders();
                var existing = providers.FirstOrDefault(p => p.Id == provider.Id);
                if (existing != null)
                {
                    providers.Remove(existing);
                }
                providers.Add(provider);
                File.WriteAllText(_filePath, JsonSerializer.Serialize(providers, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public void DeleteProvider(string id)
        {
            lock (_lock)
            {
                var providers = GetProviders();
                providers.RemoveAll(p => p.Id == id);
                File.WriteAllText(_filePath, JsonSerializer.Serialize(providers, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }
}