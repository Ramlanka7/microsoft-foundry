using AzureOpenAISample.Models;

namespace AzureOpenAISample.Services;

/// <summary>
/// Service for Azure Cognitive Search with Vector capabilities
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Create or update the search index with vector configuration
    /// </summary>
    Task CreateOrUpdateIndexAsync();

    /// <summary>
    /// Search using vectors (pure semantic search)
    /// </summary>
    Task<VectorSearchResponse> VectorSearchAsync(VectorSearchRequest request);

    /// <summary>
    /// Hybrid search (combines keyword BM25 + vector semantic search)
    /// INTERVIEW TIP: Hybrid search typically gives best results!
    /// </summary>
    Task<VectorSearchResponse> HybridSearchAsync(VectorSearchRequest request);

    /// <summary>
    /// Add document and automatically generate embeddings
    /// </summary>
    Task<string> AddDocumentAsync(VectorDocumentRequest document);

    /// <summary>
    /// Batch add documents with embeddings
    /// </summary>
    Task<List<string>> AddDocumentsBatchAsync(List<VectorDocumentRequest> documents);

    /// <summary>
    /// Delete document by ID
    /// </summary>
    Task DeleteDocumentAsync(string documentId);

    /// <summary>
    /// Get document by ID
    /// </summary>
    Task<VectorSearchDocument?> GetDocumentAsync(string documentId);
}
