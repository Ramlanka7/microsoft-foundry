using AzureOpenAISample.Models;

namespace AzureOpenAISample.Services;

/// <summary>
/// Service for Azure OpenAI interactions
/// Interview Tip: Azure OpenAI provides access to OpenAI's language models through Azure infrastructure
/// Key concepts: Chat Completions, Embeddings, Streaming, Token management
/// </summary>
public interface IAzureOpenAIService
{
    /// <summary>
    /// Get a chat completion from Azure OpenAI
    /// </summary>
    Task<ChatResponse> GetChatCompletionAsync(ChatRequest request);
    
    /// <summary>
    /// Get a streaming chat completion
    /// Interview Tip: Streaming allows real-time responses as tokens are generated
    /// </summary>
    Task<IAsyncEnumerable<string>> GetStreamingChatCompletionAsync(ChatRequest request);
    
    /// <summary>
    /// Generate embeddings for text (useful for semantic search)
    /// Interview Tip: Embeddings convert text into vector representations for similarity comparisons
    /// </summary>
    Task<float[]> GetEmbeddingsAsync(string text);
}
