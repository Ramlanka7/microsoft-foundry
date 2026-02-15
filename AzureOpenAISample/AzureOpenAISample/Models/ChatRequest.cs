namespace AzureOpenAISample.Models;

/// <summary>
/// Request model for chat completions
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 800;
    public float Temperature { get; set; } = 0.7f;
}

/// <summary>
/// Response model for chat completions
/// </summary>
public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string Model { get; set; } = string.Empty;
}
