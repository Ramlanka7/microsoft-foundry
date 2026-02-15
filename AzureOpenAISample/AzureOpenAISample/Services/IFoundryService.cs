using Azure.AI.Inference;

namespace AzureOpenAISample.Services;

public interface IFoundryService
{
    Task<string> GetChatCompletionAsync(string prompt);
}
