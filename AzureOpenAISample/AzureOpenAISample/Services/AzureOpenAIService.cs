using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using AzureOpenAISample.Models;
using Microsoft.Extensions.Options;

namespace AzureOpenAISample.Services;

/// <summary>
/// Implementation of Azure OpenAI Service
/// Interview Points:
/// 1. Uses Azure.AI.OpenAI SDK (not the OpenAI library)
/// 2. Supports both API Key and Managed Identity authentication
/// 3. Handles chat completions, streaming, and embeddings
/// 4. Proper error handling and token management
/// </summary>
public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(
        IConfiguration configuration, 
        ILogger<AzureOpenAIService> logger)
    {
        _logger = logger;
        var endpoint = configuration["AzureOpenAI:Endpoint"] 
            ?? throw new ArgumentNullException("AzureOpenAI:Endpoint is missing");
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] 
            ?? throw new ArgumentNullException("AzureOpenAI:DeploymentName is missing");

        // Interview Tip: Show both authentication methods
        var useManagedIdentity = configuration.GetValue<bool>("AzureOpenAI:UseManagedIdentity");
        
        if (useManagedIdentity)
        {
            // Use Managed Identity (recommended for production)
            _logger.LogInformation("Using Managed Identity for Azure OpenAI");
            _client = new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        }
        else
        {
            // Use API Key (for development)
            var apiKey = configuration["AzureOpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("AzureOpenAI:ApiKey is missing or empty");
            _logger.LogInformation("Using API Key for Azure OpenAI");
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }
    }

    /// <summary>
    /// Interview Talking Points:
    /// - ChatCompletionsOptions is the main request object
    /// - Messages array contains the conversation history
    /// - Temperature controls randomness (0-2, higher = more random)
    /// - MaxTokens limits the response length
    /// - Response includes token usage for cost tracking
    /// </summary>
    public async Task<ChatResponse> GetChatCompletionAsync(ChatRequest request)
    {
        try
        {
            var chatOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful AI assistant."),
                    new ChatRequestUserMessage(request.Message)
                },
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature
            };

            _logger.LogInformation("Sending chat request to Azure OpenAI");
            var response = await _client.GetChatCompletionsAsync(chatOptions);
            var choice = response.Value.Choices[0];

            return new ChatResponse
            {
                Response = choice.Message.Content,
                TokensUsed = response.Value.Usage.TotalTokens,
                Model = response.Value.Model
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure OpenAI request failed");
            throw new ApplicationException($"Azure OpenAI Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Interview Talking Points:
    /// - Streaming provides real-time response generation
    /// - Uses GetChatCompletionsStreaming for token-by-token responses
    /// - Important for better UX in chat applications
    /// - Yields control back to caller for each token
    /// </summary>
    public async Task<IAsyncEnumerable<string>> GetStreamingChatCompletionAsync(ChatRequest request)
    {
        return GetStreamingResponseAsync(request);
    }

    private async IAsyncEnumerable<string> GetStreamingResponseAsync(ChatRequest request)
    {
        var chatOptions = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage("You are a helpful AI assistant."),
                new ChatRequestUserMessage(request.Message)
            },
            MaxTokens = request.MaxTokens,
            Temperature = request.Temperature
        };

        var streamingResponse = await _client.GetChatCompletionsStreamingAsync(chatOptions);

        await foreach (var update in streamingResponse)
        {
            if (update.ContentUpdate != null)
            {
                yield return update.ContentUpdate;
            }
        }
    }

    /// <summary>
    /// Interview Talking Points:
    /// - Embeddings convert text to numerical vectors (typically 1536 dimensions)
    /// - Used for semantic search, similarity comparisons, clustering
    /// - Different model than chat (typically text-embedding-ada-002)
    /// - Vectors can be stored in Azure Cognitive Search or vector databases
    /// </summary>
    public async Task<float[]> GetEmbeddingsAsync(string text)
    {
        try
        {
            var embeddingsOptions = new EmbeddingsOptions("text-embedding-ada-002", new[] { text });
            var response = await _client.GetEmbeddingsAsync(embeddingsOptions);
            
            return response.Value.Data[0].Embedding.ToArray();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings");
            throw new ApplicationException($"Embeddings Error: {ex.Message}", ex);
        }
    }
}
