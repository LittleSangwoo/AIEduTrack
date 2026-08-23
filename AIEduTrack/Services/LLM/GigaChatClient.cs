using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIEduTrack.Models;

namespace AIEduTrack.Services.LLM
{
    public class GigaChatClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly LlmProviderConfig _config;

        public string ProviderName => _config.Name;

        public GigaChatClient(HttpClient httpClient, LlmProviderConfig config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> GenerateResponseAsync(string systemContext, string userPrompt)
        {
            // 1. Получаем временный токен авторизации
            var token = await GetAccessTokenAsync();

            // 2. Отправляем промпт
            var request = new HttpRequestMessage(HttpMethod.Post, _config.BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                model = _config.Model,
                messages = new[]
                {
                    new { role = "system", content = systemContext },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1
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

        private async Task<string> GetAccessTokenAsync()
        {
            // GigaChat требует уникальный RqUID для каждого запроса токена
            var rqUid = Guid.NewGuid().ToString();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");
            request.Headers.Add("RqUID", rqUid);

            // В твоем JSON токен авторизации лежит в поле Scope (Guid)
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _config.Scope);

            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        }
    }
}