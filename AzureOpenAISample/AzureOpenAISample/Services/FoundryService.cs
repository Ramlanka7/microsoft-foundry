using Azure;
using Azure.AI.Inference;
using Azure.Identity;

namespace AzureOpenAISample.Services;

public class FoundryService : IFoundryService
{
    private readonly ChatCompletionsClient? _client;
    private readonly string _modelName;
    private readonly ILogger<FoundryService> _logger;

    public FoundryService(IConfiguration configuration, ILogger<FoundryService> logger)
    {
        _logger = logger;
        
        var endpoint = configuration["Foundry:Endpoint"];
        var apiKey = configuration["Foundry:ApiKey"];
        _modelName = configuration["Foundry:ModelName"] ?? "Phi-3-mini-4k-instruct";

        if (string.IsNullOrEmpty(endpoint))
        {
            _logger.LogWarning("Foundry:Endpoint is not configured.");
        }
        else if (!string.IsNullOrEmpty(apiKey))
        {
             _client = new ChatCompletionsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }
        else
        {
            _client = new ChatCompletionsClient(new Uri(endpoint), new DefaultAzureCredential());
        }
    }

    public async Task<string> GetChatCompletionAsync(string prompt)
    {
        if (_client == null)
        {
            return "Foundry service is not configured correctly. Please check appsettings.json.";
        }

        try
        {
            var requestOptions = new Azure.AI.Inference.ChatCompletionsOptions()
            {
                Messages =
                {
                    new Azure.AI.Inference.ChatRequestSystemMessage("You are a helpful AI assistant."),
                    new Azure.AI.Inference.ChatRequestUserMessage(prompt),
                },
                Model = _modelName,
                Temperature = 0.7f,
                MaxTokens = 1000
            };

            Response<Azure.AI.Inference.ChatCompletions> response = await _client.CompleteAsync(requestOptions);
            
            // Checking if Choices exists by using fully qualified name, or maybe it's just 'Content' for simple cases?
            // Actually, for Azure.AI.Inference, it should be Choices. 
            // If it fails again, I'll inspect the error more closely.
            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Foundry endpoint");
            return $"Error: {ex.Message}";
        }
    }
}
