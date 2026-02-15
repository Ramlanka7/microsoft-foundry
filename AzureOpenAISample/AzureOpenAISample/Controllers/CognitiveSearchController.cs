using AzureOpenAISample.Models;
using AzureOpenAISample.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureOpenAISample.Controllers;

/// <summary>
/// Controller demonstrating Azure Cognitive Search operations
/// 
/// Interview Preparation Topics:
/// 1. Explain indexing vs searching
/// 2. Discuss ranking and relevance scoring (BM25 algorithm)
/// 3. Explain semantic search vs keyword search
/// 4. Discuss faceting and filtering (OData syntax)
/// 5. Talk about pagination and performance optimization
/// 6. Integration with Azure OpenAI for vector/semantic search
/// </summary>
[ApiController]
[Route("api/CognitiveSearch")]
public class CognitiveSearchController : ControllerBase
{
    private readonly IAzureCognitiveSearchService _searchService;
    private readonly ILogger<CognitiveSearchController> _logger;

    public CognitiveSearchController(
        IAzureCognitiveSearchService searchService,
        ILogger<CognitiveSearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// POST: api/CognitiveSearch/search
    /// Search for documents
    /// 
    /// Example Request:
    /// {
    ///   "searchText": ".NET Core",
    ///   "top": 10,
    ///   "filter": "category eq 'Technology'",
    ///   "facets": ["category", "tags"]
    /// }
    /// 
    /// Interview Tip: Explain how to use filters and facets for drilling down results
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchQuery query)
    {
        try
        {
            var results = await _searchService.SearchAsync(query);
            return Ok(new
            {
                totalCount = results.TotalCount,
                results = results.Documents,
                facets = results.Facets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/CognitiveSearch/index
    /// Index a single document
    /// 
    /// Example Request:
    /// {
    ///   "id": "1",
    ///   "title": "Introduction to .NET 8",
    ///   "content": "Learn about the new features in .NET 8...",
    ///   "category": "Technology",
    ///   "createdDate": "2024-01-15T00:00:00Z",
    ///   "tags": ["dotnet", "csharp", "programming"]
    /// }
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> IndexDocument([FromBody] SearchDocument document)
    {
        if (string.IsNullOrEmpty(document.Id))
        {
            return BadRequest("Document ID is required");
        }

        try
        {
            var success = await _searchService.IndexDocumentAsync(document);
            return Ok(new { success, message = "Document indexed successfully", documentId = document.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indexing failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/CognitiveSearch/index-batch
    /// Index multiple documents at once
    /// Interview Tip: Batching is more efficient for large-scale indexing
    /// </summary>
    [HttpPost("index-batch")]
    public async Task<IActionResult> IndexDocuments([FromBody] List<SearchDocument> documents)
    {
        if (!documents.Any())
        {
            return BadRequest("At least one document is required");
        }

        try
        {
            var success = await _searchService.IndexDocumentsAsync(documents);
            return Ok(new { success, count = documents.Count, message = "Documents indexed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch indexing failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/CognitiveSearch/seed-sample-data
    /// Create sample documents for testing
    /// Interview Tip: Useful for demonstrating functionality without external data
    /// </summary>
    [HttpGet("seed-sample-data")]
    public async Task<IActionResult> SeedSampleData()
    {
        try
        {
            var sampleDocuments = new List<SearchDocument>
            {
                new SearchDocument
                {
                    Id = "1",
                    Title = "Getting Started with .NET 8",
                    Content = "NET 8 is the latest long-term support release of .NET, featuring improved performance and new features.",
                    Category = "Technology",
                    CreatedDate = DateTime.UtcNow.AddDays(-10),
                    Tags = new[] { "dotnet", "csharp", "programming" }
                },
                new SearchDocument
                {
                    Id = "2",
                    Title = "Azure OpenAI Best Practices",
                    Content = "Learn how to effectively use Azure OpenAI service for building intelligent applications.",
                    Category = "AI",
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    Tags = new[] { "azure", "openai", "ai" }
                },
                new SearchDocument
                {
                    Id = "3",
                    Title = "Cognitive Search for Developers",
                    Content = "Azure Cognitive Search provides AI-powered search capabilities including semantic search and vector search.",
                    Category = "Azure",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    Tags = new[] { "azure", "search", "cognitive-services" }
                },
                new SearchDocument
                {
                    Id = "4",
                    Title = "Managed Identity in Azure",
                    Content = "Use Managed Identity to eliminate the need for credentials in your code when accessing Azure services.",
                    Category = "Security",
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    Tags = new[] { "azure", "security", "identity" }
                }
            };

            var success = await _searchService.IndexDocumentsAsync(sampleDocuments);
            return Ok(new 
            { 
                success, 
                count = sampleDocuments.Count, 
                message = "Sample data indexed successfully",
                tip = "Try searching for '.NET', 'Azure', or filtering by category" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed sample data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET: api/CognitiveSearch/document/{id}
    /// Get a specific document by ID
    /// </summary>
    [HttpGet("document/{id}")]
    public async Task<IActionResult> GetDocument(string id)
    {
        try
        {
            var document = await _searchService.GetDocumentAsync(id);
            if (document == null)
            {
                return NotFound(new { message = $"Document with ID '{id}' not found" });
            }
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE: api/CognitiveSearch/document/{id}
    /// Delete a document from the index
    /// </summary>
    [HttpDelete("document/{id}")]
    public async Task<IActionResult> DeleteDocument(string id)
    {
        try
        {
            var success = await _searchService.DeleteDocumentAsync(id);
            if (success)
            {
                return Ok(new { success = true, message = $"Document '{id}' deleted successfully" });
            }
            return NotFound(new { message = $"Document with ID '{id}' not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
