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
            var rqUid = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");

            request.Headers.Add("RqUID", rqUid);

            // 1. Очищаем ключ от мусора (пробелы, переносы строк, случайное слово Basic)
            var cleanKey = _config.ApiKey?.Replace("Basic ", "", StringComparison.OrdinalIgnoreCase).Trim();
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", cleanKey);

            // 2. Делаем отправку scope в точности как в твоем старом проекте
            var scope = string.IsNullOrWhiteSpace(_config.Scope) ? "GIGACHAT_API_PERS" : _config.Scope.Trim();
            request.Content = new StringContent($"scope={scope}", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request);

            // 3. Если Сбер снова кинет ошибку, выводим её ЧИТАЕМЫЙ текст, чтобы не гадать вслепую
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Отказ Сбера ({response.StatusCode}): {errorBody}");
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        }
    }
}