using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using AzureOpenAISample.Models;

namespace AzureOpenAISample.Services;

/// <summary>
/// Azure Cognitive Search with Vector Search capabilities
/// 
/// INTERVIEW TALKING POINTS:
/// 1. Vector Search = Semantic similarity using embeddings (not just keyword matching)
/// 2. HNSW Algorithm = Hierarchical Navigable Small World (fast approximate nearest neighbor)
/// 3. Cosine Similarity = Measures angle between vectors (0=orthogonal, 1=identical)
/// 4. Hybrid Search = BM25(text) + HNSW(vectors) = Best of both worlds
/// 5. Why Hybrid? Text catches exact terms, vectors catch semantic meaning
/// 6. Embedding Dimensions must match: text-embedding-ada-002 = 1536
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private readonly IAzureOpenAIService _openAIService;
    private readonly ILogger<VectorSearchService> _logger;
    private readonly string _indexName;

    public VectorSearchService(
        IConfiguration configuration,
        IAzureOpenAIService openAIService,
        ILogger<VectorSearchService> logger)
    {
        _openAIService = openAIService;
        _logger = logger;

        var endpoint = new Uri(configuration["AzureCognitiveSearch:VectorIndexEndpoint"] 
            ?? configuration["AzureCognitiveSearch:Endpoint"] 
            ?? throw new ArgumentNullException("Search endpoint not configured"));
        
        _indexName = configuration["AzureCognitiveSearch:VectorIndexName"] ?? "vector-index";

        var useManagedIdentity = configuration.GetValue<bool>("AzureCognitiveSearch:UseManagedIdentity");

        if (useManagedIdentity)
        {
            _logger.LogInformation("Using Managed Identity for Vector Search");
            var credential = new DefaultAzureCredential();
            _searchClient = new SearchClient(endpoint, _indexName, credential);
            _indexClient = new SearchIndexClient(endpoint, credential);
        }
        else
        {
            var apiKey = configuration["AzureCognitiveSearch:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("Search API Key is missing or empty");
            _logger.LogInformation("Using API Key for Vector Search");
            var credential = new AzureKeyCredential(apiKey);
            _searchClient = new SearchClient(endpoint, _indexName, credential);
            _indexClient = new SearchIndexClient(endpoint, credential);
        }
    }

    /// <summary>
    /// Create search index with vector configuration
    /// 
    /// INTERVIEW KEY CONCEPTS:
    /// 1. VectorSearch configuration defines algorithm and parameters
    /// 2. HNSW parameters:
    ///    - m: Max bidirectional links per node (higher = better recall, slower)
    ///    - efConstruction: Size of dynamic candidate list (higher = better quality)
    ///    - efSearch: Dynamic candidate list size at search (higher = better recall)
    /// 3. Cosine similarity best for normalized embeddings (OpenAI embeddings are normalized)
    /// 4. Can have multiple vector profiles for different use cases
    /// </summary>
    public async Task CreateOrUpdateIndexAsync()
    {
        try
        {
            _logger.LogInformation("Creating/Updating vector search index: {IndexName}", _indexName);

            // Define vector search configuration
            var vectorSearch = new VectorSearch();
            
            // Add HNSW algorithm configuration
            vectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration("hnsw-config")
            {
                Parameters = new HnswParameters
                {
                    M = 4,                    // Number of bi-directional links (default: 4, range: 4-10)
                    EfConstruction = 400,     // Size of dynamic candidate list for construction (default: 400)
                    EfSearch = 500,           // Size of dynamic candidate list for search (default: 500)
                    Metric = VectorSearchAlgorithmMetric.Cosine  // Cosine similarity for text embeddings
                }
            });

            // Add vector profile linking algorithm to field
            vectorSearch.Profiles.Add(new VectorSearchProfile("my-vector-profile", "hnsw-config"));

            // Define index schema
            var index = new SearchIndex(_indexName)
            {
                Fields =
                {
                    new SimpleField("Id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SearchableField("Title") { IsFilterable = true, IsSortable = true },
                    new SearchableField("Content"),
                    new SearchableField("Category") { IsFilterable = true, IsFacetable = true },
                    new SimpleField("CreatedDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                    new SearchableField("Tags", collection: true) { IsFilterable = true, IsFacetable = true },
                    new SimpleField("SourceUrl", SearchFieldDataType.String),
                    new SimpleField("TokenCount", SearchFieldDataType.Int32),
                    
                    // CRITICAL: Vector field for embeddings
                    new SearchField("ContentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = 1536,  // text-embedding-ada-002 dimensions
                        VectorSearchProfileName = "my-vector-profile"
                    }
                },
                VectorSearch = vectorSearch
            };

            // Create or update index
            await _indexClient.CreateOrUpdateIndexAsync(index);
            _logger.LogInformation("Vector search index created/updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create/update vector search index");
            throw;
        }
    }

    /// <summary>
    /// Pure vector search using semantic similarity
    /// 
    /// INTERVIEW POINTS:
    /// - Converts query to embedding using OpenAI
    /// - Finds documents with similar embeddings (cosine similarity)
    /// - Best for semantic/conceptual search (synonyms, paraphrases)
    /// - May miss exact keyword matches
    /// </summary>
    public async Task<VectorSearchResponse> VectorSearchAsync(VectorSearchRequest request)
    {
        try
        {
            // 1. Generate embedding for query
            var queryEmbedding = await _openAIService.GetEmbeddingsAsync(request.Query);
            
            // 2. Configure vector search
            var searchOptions = new SearchOptions
            {
                VectorSearch = new()
                {
                    Queries = { new VectorizedQuery(queryEmbedding.ToArray()) { KNearestNeighborsCount = request.Top, Fields = { "ContentVector" } } }
                },
                Size = request.Top,
                Select = { "Id", "Title", "Content", "Category", "CreatedDate", "Tags", "SourceUrl" }
            };

            // Add filter if provided
            if (!string.IsNullOrEmpty(request.Filter))
            {
                searchOptions.Filter = request.Filter;
            }

            // 3. Execute search
            _logger.LogInformation("Executing vector search: {Query}", request.Query);
            var response = await _searchClient.SearchAsync<VectorSearchDocument>(null, searchOptions);

            // 4. Build response
            var results = new List<VectorSearchResult>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                results.Add(new VectorSearchResult
                {
                    Document = result.Document,
                    Score = result.Score ?? 0,
                    VectorScore = result.Score
                });
            }

            return new VectorSearchResponse
            {
                Results = results,
                TotalCount = results.Count,
                SearchMode = AzureOpenAISample.Models.SearchMode.Vector
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed");
            throw;
        }
    }

    /// <summary>
    /// Hybrid search: combines keyword (BM25) + vector (semantic) search
    /// 
    /// INTERVIEW GOLD STAR:
    /// "Hybrid search gives best results because:
    /// 1. BM25 catches exact keywords, technical terms, proper nouns
    /// 2. Vector search catches semantic meaning, synonyms, paraphrases
    /// 3. Reciprocal Rank Fusion (RRF) merges both result sets intelligently
    /// 4. Production systems typically use hybrid, not pure vector"
    /// 
    /// Example: Query "ML model training"
    /// - BM25 finds: exact phrase "ML model training"
    /// - Vector finds: "machine learning algorithm development" (semantically similar)
    /// - Hybrid returns both!
    /// </summary>
    public async Task<VectorSearchResponse> HybridSearchAsync(VectorSearchRequest request)
    {
        try
        {
            // 1. Generate embedding for query
            var queryEmbedding = await _openAIService.GetEmbeddingsAsync(request.Query);
            
            // 2. Configure hybrid search (text + vector)
            var searchOptions = new SearchOptions
            {
                // Text search configuration (BM25)
                SearchMode = Azure.Search.Documents.Models.SearchMode.All,
                QueryType = SearchQueryType.Simple,
                
                // Vector search configuration
                VectorSearch = new()
                {
                    Queries = { new VectorizedQuery(queryEmbedding.ToArray()) 
                    { 
                        KNearestNeighborsCount = request.Top * 2,  // Get more candidates for fusion
                        Fields = { "ContentVector" } 
                    } }
                },
                
                Size = request.Top,
                Select = { "Id", "Title", "Content", "Category", "CreatedDate", "Tags", "SourceUrl" }
            };

            // Add filter if provided
            if (!string.IsNullOrEmpty(request.Filter))
            {
                searchOptions.Filter = request.Filter;
            }

            // 3. Execute hybrid search (both text and vector)
            _logger.LogInformation("Executing hybrid search: {Query}", request.Query);
            var response = await _searchClient.SearchAsync<VectorSearchDocument>(request.Query, searchOptions);

            // 4. Build response with both scores
            var results = new List<VectorSearchResult>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                results.Add(new VectorSearchResult
                {
                    Document = result.Document,
                    Score = result.Score ?? 0,
                    // Note: Azure Search automatically combines scores using RRF
                });
            }

            return new VectorSearchResponse
            {
                Results = results,
                TotalCount = results.Count,
                SearchMode = AzureOpenAISample.Models.SearchMode.Hybrid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hybrid search failed");
            throw;
        }
    }

    /// <summary>
    /// Add document with auto-generated embeddings
    /// </summary>
    public async Task<string> AddDocumentAsync(VectorDocumentRequest document)
    {
        try
        {
            var docId = Guid.NewGuid().ToString();
            
            // Generate embedding for content
            var embedding = await _openAIService.GetEmbeddingsAsync(document.Content);

            var vectorDoc = new VectorSearchDocument
            {
                Id = docId,
                Title = document.Title,
                Content = document.Content,
                Category = document.Category,
                CreatedDate = DateTime.UtcNow,
                Tags = document.Tags,
                SourceUrl = document.SourceUrl,
                ContentVector = embedding.ToArray(),
                TokenCount = document.Content.Split(' ').Length
            };

            await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(new[] { vectorDoc }));
            _logger.LogInformation("Document indexed with embeddings: {DocumentId}", docId);
            
            return docId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add document");
            throw;
        }
    }

    /// <summary>
    /// Batch add documents with embeddings
    /// INTERVIEW TIP: Always batch when adding multiple documents (more efficient)
    /// </summary>
    public async Task<List<string>> AddDocumentsBatchAsync(List<VectorDocumentRequest> documents)
    {
        try
        {
            var vectorDocs = new List<VectorSearchDocument>();
            var docIds = new List<string>();

            foreach (var doc in documents)
            {
                var docId = Guid.NewGuid().ToString();
                docIds.Add(docId);

                // Generate embedding
                var embedding = await _openAIService.GetEmbeddingsAsync(doc.Content);

                vectorDocs.Add(new VectorSearchDocument
                {
                    Id = docId,
                    Title = doc.Title,
                    Content = doc.Content,
                    Category = doc.Category,
                    CreatedDate = DateTime.UtcNow,
                    Tags = doc.Tags,
                    SourceUrl = doc.SourceUrl,
                    ContentVector = embedding.ToArray(),
                    TokenCount = doc.Content.Split(' ').Length
                });
            }

            // Batch upload (max 1000 documents per batch)
            await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(vectorDocs));
            _logger.LogInformation("Batch indexed {Count} documents with embeddings", documents.Count);

            return docIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch add documents");
            throw;
        }
    }

    public async Task DeleteDocumentAsync(string documentId)
    {
        try
        {
            await _searchClient.DeleteDocumentsAsync("Id", new[] { documentId });
            _logger.LogInformation("Document deleted: {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document");
            throw;
        }
    }

    public async Task<VectorSearchDocument?> GetDocumentAsync(string documentId)
    {
        try
        {
            var response = await _searchClient.GetDocumentAsync<VectorSearchDocument>(documentId);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document");
            throw;
        }
    }
}
