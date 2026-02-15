using AzureOpenAISample.Models;

namespace AzureOpenAISample.Services;

/// <summary>
/// Service for Azure Cognitive Search operations
/// 
/// Interview Topics:
/// - Full-text search with ranking
/// - Semantic search (AI-powered understanding)
/// - Faceting and filtering
/// - Indexing and index management
/// - Vector search for embeddings
/// </summary>
public interface IAzureCognitiveSearchService
{
    /// <summary>
    /// Search documents in the index
    /// </summary>
    Task<SearchResults> SearchAsync(SearchQuery query);
    
    /// <summary>
    /// Upload/Index a document
    /// </summary>
    Task<bool> IndexDocumentAsync(AzureOpenAISample.Models.SearchDocument document);
    
    /// <summary>
    /// Upload multiple documents
    /// </summary>
    Task<bool> IndexDocumentsAsync(IEnumerable<AzureOpenAISample.Models.SearchDocument> documents);
    
    /// <summary>
    /// Delete a document by ID
    /// </summary>
    Task<bool> DeleteDocumentAsync(string documentId);
    
    /// <summary>
    /// Get a specific document by ID
    /// </summary>
    Task<AzureOpenAISample.Models.SearchDocument?> GetDocumentAsync(string documentId);
}
