namespace AIEduTrack.Models
{
    public class LlmProviderConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsLocal { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? Scope { get; set; }
        public string AuthType { get; set; } = string.Empty; // "OpenAI" или "GigaChat"
    }
}