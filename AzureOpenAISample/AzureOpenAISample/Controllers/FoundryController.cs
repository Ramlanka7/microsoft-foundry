using AzureOpenAISample.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureOpenAISample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoundryController : ControllerBase
{
    private readonly IFoundryService _foundryService;
    private readonly ILogger<FoundryController> _logger;

    public FoundryController(IFoundryService foundryService, ILogger<FoundryController> logger)
    {
        _foundryService = foundryService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a prompt to the configured Foundry model and returns the response.
    /// </summary>
    /// <param name="request">The chat request containing the prompt.</param>
    /// <returns>The AI's response.</returns>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] FoundryChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("Prompt is required.");
        }

        try
        {
            var response = await _foundryService.GetChatCompletionAsync(request.Prompt);
            return Ok(new { response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Foundry chat request");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    public class FoundryChatRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }
}
