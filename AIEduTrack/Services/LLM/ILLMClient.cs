namespace AIEduTrack.Services.LLM
{
    public interface ILLMClient
    {
        string ProviderName { get; }
        Task<string> GenerateResponseAsync(string systemContext, string userPrompt);
    }
}
