using AzureOpenAISample.Models;
using AzureOpenAISample.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureOpenAISample.Controllers;

/// <summary>
/// Controller demonstrating Azure OpenAI integration
/// 
/// Interview Preparation:
/// 1. Explain the difference between Azure OpenAI and OpenAI API
/// 2. Discuss token limits and pricing
/// 3. Explain streaming vs non-streaming responses
/// 4. Discuss use cases: chat, content generation, summarization, embeddings
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AzureOpenAIController : ControllerBase
{
    private readonly IAzureOpenAIService _openAIService;
    private readonly ILogger<AzureOpenAIController> _logger;

    public AzureOpenAIController(
        IAzureOpenAIService openAIService, 
        ILogger<AzureOpenAIController> logger)
    {
        _openAIService = openAIService;
        _logger = logger;
    }

    /// <summary>
    /// GET: api/AzureOpenAI/test
    /// Simple test endpoint to verify Azure OpenAI connectivity
    /// </summary>
    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var request = new ChatRequest 
            { 
                Message = "Say 'Hello, I am Azure OpenAI!' in a friendly way.",
                MaxTokens = 100
            };
            
            var response = await _openAIService.GetChatCompletionAsync(request);
            return Ok(new { 
                success = true, 
                message = response.Response,
                tokensUsed = response.TokensUsed,
                model = response.Model
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test connection failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/AzureOpenAI/chat
    /// Standard chat completion endpoint
    /// 
    /// Example Request:
    /// {
    ///   "message": "Explain dependency injection in .NET",
    ///   "maxTokens": 500,
    ///   "temperature": 0.7
    /// }
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Message is required");
        }

        try
        {
            _logger.LogInformation("Processing chat request");
            var response = await _openAIService.GetChatCompletionAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat request failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/AzureOpenAI/chat-stream
    /// Streaming chat completion endpoint
    /// 
    /// Interview Tip: Explain Server-Sent Events (SSE) pattern for streaming
    /// Response is sent in real-time as tokens are generated
    /// </summary>
    [HttpPost("chat-stream")]
    public async Task ChatStream([FromBody] ChatRequest request)
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        try
        {
            var streamingResponse = await _openAIService.GetStreamingChatCompletionAsync(request);
            
            await foreach (var token in streamingResponse)
            {
                await Response.WriteAsync($"data: {token}\n\n");
                await Response.Body.FlushAsync();
            }
            
            await Response.WriteAsync("data: [DONE]\n\n");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming chat failed");
            await Response.WriteAsync($"data: Error: {ex.Message}\n\n");
        }
    }

    /// <summary>
    /// POST: api/AzureOpenAI/embeddings
    /// Generate embeddings for text
    /// 
    /// Interview Topics:
    /// - Embeddings are vector representations of text (typically 1536 dimensions)
    /// - Used for semantic search, recommendations, clustering
    /// - Can be stored in vector databases or Azure Cognitive Search
    /// 
    /// Example Request:
    /// {
    ///   "text": "Azure OpenAI is a managed service for OpenAI models"
    /// }
    /// </summary>
    [HttpPost("embeddings")]
    public async Task<IActionResult> GetEmbeddings([FromBody] EmbeddingRequest request)
    {
        if (string.IsNullOrEmpty(request.Text))
        {
            return BadRequest("Text is required");
        }

        try
        {
            var embeddings = await _openAIService.GetEmbeddingsAsync(request.Text);
            return Ok(new 
            { 
                text = request.Text,
                dimensions = embeddings.Length,
                embeddings = embeddings.Take(10), // Show only first 10 values for brevity
                message = $"Full embedding has {embeddings.Length} dimensions"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Embeddings request failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class EmbeddingRequest
{
    public string Text { get; set; } = string.Empty;
}
