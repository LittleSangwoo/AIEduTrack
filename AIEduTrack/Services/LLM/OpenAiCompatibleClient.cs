using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIEduTrack.Models;

namespace AIEduTrack.Services.LLM
{
    public class OpenAiCompatibleClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly LlmProviderConfig _config;

        public string ProviderName => _config.Name;

        public OpenAiCompatibleClient(HttpClient httpClient, LlmProviderConfig config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> GenerateResponseAsync(string systemContext, string userPrompt)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _config.BaseUrl);

            // Если есть ключ (не пустой и не "-"), добавляем его в заголовки
            if (!string.IsNullOrWhiteSpace(_config.ApiKey) && _config.ApiKey != "-")
            {
                // Для YandexGPT иногда требуется "Api-Key", но OpenAI стандарт требует Bearer
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
            }

            var payload = new
            {
                model = _config.Model,
                messages = new[]
                {
                    new { role = "system", content = systemContext },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1 // Низкая температура для более строгих JSON-ответов
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
    }
}