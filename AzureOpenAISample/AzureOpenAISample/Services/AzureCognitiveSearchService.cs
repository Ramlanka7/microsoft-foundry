using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using AzureOpenAISample.Models;

namespace AzureOpenAISample.Services;

/// <summary>
/// Implementation of Azure Cognitive Search Service
/// 
/// Interview Key Points:
/// 1. SearchClient - for querying and document operations
/// 2. SearchIndexClient - for index management (create/update schema)
/// 3. Supports both API Key and Managed Identity
/// 4. Full-text search with BM25 ranking algorithm
/// 5. Can integrate with Azure OpenAI for semantic/vector search
/// </summary>
public class AzureCognitiveSearchService : IAzureCognitiveSearchService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger<AzureCognitiveSearchService> _logger;

    public AzureCognitiveSearchService(
        IConfiguration configuration,
        ILogger<AzureCognitiveSearchService> logger)
    {
        _logger = logger;
        var endpoint = new Uri(configuration["AzureCognitiveSearch:Endpoint"] 
            ?? throw new ArgumentNullException("AzureCognitiveSearch:Endpoint"));
        var indexName = configuration["AzureCognitiveSearch:IndexName"] 
            ?? throw new ArgumentNullException("AzureCognitiveSearch:IndexName");

        var useManagedIdentity = configuration.GetValue<bool>("AzureCognitiveSearch:UseManagedIdentity");

        if (useManagedIdentity)
        {
            _logger.LogInformation("Using Managed Identity for Cognitive Search");
            _searchClient = new SearchClient(endpoint, indexName, new DefaultAzureCredential());
        }
        else
        {
            var apiKey = configuration["AzureCognitiveSearch:ApiKey"] 
                ?? throw new ArgumentNullException("AzureCognitiveSearch:ApiKey");
            _logger.LogInformation("Using API Key for Cognitive Search");
            _searchClient = new SearchClient(endpoint, indexName, new AzureKeyCredential(apiKey));
        }
    }

    /// <summary>
    /// Interview Talking Points:
    /// - SearchOptions configures the search behavior
    /// - IncludeTotalCount returns total matching documents (useful for pagination)
    /// - Filter uses OData syntax: "category eq 'Technology' and createdDate gt 2024-01-01"
    /// - Select specifies which fields to return
    /// - Facets enable faceted navigation (like filters in e-commerce)
    /// - OrderBy sorts results (default is by relevance score @search.score)
    /// </summary>
    public async Task<SearchResults> SearchAsync(SearchQuery query)
    {
        try
        {
            var searchOptions = new SearchOptions
            {
                IncludeTotalCount = true,
                Size = query.Top
            };

            // Add filter if provided
            if (!string.IsNullOrEmpty(query.Filter))
            {
                searchOptions.Filter = query.Filter;
            }

            // Add facets if provided
            if (query.Facets?.Any() == true)
            {
                foreach (var facet in query.Facets)
                {
                    searchOptions.Facets.Add(facet);
                }
            }

            _logger.LogInformation("Executing search: {SearchText}", query.SearchText);
            var response = await _searchClient.SearchAsync<AzureOpenAISample.Models.SearchDocument>(query.SearchText, searchOptions);

            var results = new SearchResults
            {
                TotalCount = response.Value.TotalCount ?? 0,
                Documents = new List<AzureOpenAISample.Models.SearchDocument>()
            };

            await foreach (var result in response.Value.GetResultsAsync())
            {
                results.Documents.Add(result.Document);
            }

            // Process facets if requested
            if (response.Value.Facets?.Any() == true)
            {
                results.Facets = new Dictionary<string, List<AzureOpenAISample.Models.FacetResult>>();
                foreach (var facet in response.Value.Facets)
                {
                    results.Facets[facet.Key] = facet.Value.Select(f => new AzureOpenAISample.Models.FacetResult
                    {
                        Value = f.Value,
                        Count = f.Count ?? 0
                    }).ToList();
                }
            }

            return results;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Search operation failed");
            throw new ApplicationException($"Search Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Interview Talking Points:
    /// - UploadDocuments adds or updates documents (upsert behavior)
    /// - MergeDocuments updates only specified fields
    /// - IndexDocumentsBatch can mix upload/merge/delete operations
    /// - Indexing is near real-time (typically 1-2 seconds)
    /// - Batch operations are more efficient than individual uploads
    /// </summary>
    public async Task<bool> IndexDocumentAsync(AzureOpenAISample.Models.SearchDocument document)
    {
        try
        {
            _logger.LogInformation("Indexing document: {DocumentId}", document.Id);
            var batch = IndexDocumentsBatch.Upload(new[] { document });
            var result = await _searchClient.IndexDocumentsAsync(batch);
            
            return result.Value.Results[0].Succeeded;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to index document");
            throw new ApplicationException($"Index Error: {ex.Message}", ex);
        }
    }

    public async Task<bool> IndexDocumentsAsync(IEnumerable<AzureOpenAISample.Models.SearchDocument> documents)
    {
        try
        {
            _logger.LogInformation("Indexing {Count} documents", documents.Count());
            var batch = IndexDocumentsBatch.Upload(documents);
            var result = await _searchClient.IndexDocumentsAsync(batch);
            
            return result.Value.Results.All(r => r.Succeeded);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to index documents");
            throw new ApplicationException($"Batch Index Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Delete a document from the index
    /// Interview Tip: Explain soft vs hard deletes in search scenarios
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(string documentId)
    {
        try
        {
            _logger.LogInformation("Deleting document: {DocumentId}", documentId);
            var batch = IndexDocumentsBatch.Delete("id", new[] { documentId });
            var result = await _searchClient.IndexDocumentsAsync(batch);
            
            return result.Value.Results[0].Succeeded;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete document");
            throw new ApplicationException($"Delete Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieve a specific document by ID
    /// Interview Tip: This is a direct lookup, not a search operation
    /// </summary>
    public async Task<AzureOpenAISample.Models.SearchDocument?> GetDocumentAsync(string documentId)
    {
        try
        {
            var response = await _searchClient.GetDocumentAsync<AzureOpenAISample.Models.SearchDocument>(documentId);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to get document");
            throw new ApplicationException($"Get Document Error: {ex.Message}", ex);
        }
    }
}
