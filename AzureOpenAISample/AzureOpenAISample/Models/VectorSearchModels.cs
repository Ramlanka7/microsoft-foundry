using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace AzureOpenAISample.Models;

/// <summary>
/// Document model with vector embeddings for semantic search
/// 
/// Interview Key Points:
/// 1. [VectorSearch] attribute configures vector field for similarity search
/// 2. Dimensions must match your embedding model (1536 for text-embedding-ada-002)
/// 3. VectorSearchProfile defines the algorithm (HNSW = Hierarchical Navigable Small World)
/// 4. Distance metric: Cosine similarity (most common for text embeddings)
/// </summary>
public class VectorSearchDocument
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    public string Id { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true, IsSortable = true)]
    public string Title { get; set; } = string.Empty;

    [SearchableField]
    public string Content { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true, IsFacetable = true)]
    public string Category { get; set; } = string.Empty;

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public DateTime CreatedDate { get; set; }

    [SearchableField(IsFilterable = true, IsFacetable = true)]
    public string[]? Tags { get; set; }

    /// <summary>
    /// Vector embedding field for semantic search
    /// CRITICAL: Must use float[] and dimensions must match embedding model
    /// text-embedding-ada-002 = 1536 dimensions
    /// text-embedding-3-small = 1536 dimensions
    /// text-embedding-3-large = 3072 dimensions
    /// </summary>
    [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = "my-vector-profile")]
    public float[]? ContentVector { get; set; }

    [SimpleField]
    public string? SourceUrl { get; set; }

    [SimpleField]
    public int? TokenCount { get; set; }
}

/// <summary>
/// Request model for vector search
/// </summary>
public class VectorSearchRequest
{
    /// <summary>
    /// The query text (will be converted to embedding)
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Number of results to return
    /// </summary>
    public int Top { get; set; } = 5;

    /// <summary>
    /// OData filter (e.g., "category eq 'Technology'")
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Search mode: Vector, Text, or Hybrid
    /// </summary>
    public SearchMode Mode { get; set; } = SearchMode.Hybrid;

    /// <summary>
    /// For hybrid search: weight of text vs vector (0.0 = all vector, 1.0 = all text)
    /// </summary>
    public double TextWeight { get; set; } = 0.5;
}

public enum SearchMode
{
    Vector,      // Pure semantic search using embeddings
    Text,        // Traditional keyword search (BM25)
    Hybrid       // Combination of both (best results!)
}

/// <summary>
/// Vector search result with relevance scores
/// </summary>
public class VectorSearchResult
{
    public VectorSearchDocument Document { get; set; } = new();
    
    /// <summary>
    /// Relevance score (higher = more relevant)
    /// Cosine similarity ranges from -1 to 1, typically 0.7+ is good match
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Individual scores when using hybrid search
    /// </summary>
    public double? VectorScore { get; set; }
    public double? TextScore { get; set; }
}

/// <summary>
/// Response containing vector search results
/// </summary>
public class VectorSearchResponse
{
    public List<VectorSearchResult> Results { get; set; } = new();
    public long TotalCount { get; set; }
    public SearchMode SearchMode { get; set; }
    public string? SemanticAnswer { get; set; } // If semantic ranking is enabled
}

/// <summary>
/// Request to add document with auto-generated embeddings
/// </summary>
public class VectorDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string[]? Tags { get; set; }
    public string? SourceUrl { get; set; }
}
