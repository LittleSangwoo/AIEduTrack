namespace AIEduTrack.Services.LLM
{
    public interface ILLMFactory
    {
        ILLMClient GetClient(string providerType); // "Local", "Russian", "Foreign"
    }
}
