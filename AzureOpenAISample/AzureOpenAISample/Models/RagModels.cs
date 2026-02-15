namespace AzureOpenAISample.Models;

/// <summary>
/// RAG query request
/// Interview Tip: RAG = Retrieval Augmented Generation
/// Combines search results with LLM generation for factually grounded responses
/// </summary>
public class RagQueryRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxSearchResults { get; set; } = 3;
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.7f;
    public bool IncludeSourceReferences { get; set; } = true;
}

/// <summary>
/// RAG query response with generated answer and sources
/// </summary>
public class RagQueryResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SourceReference> Sources { get; set; } = new();
    public int TokensUsed { get; set; }
    public string SearchQuery { get; set; } = string.Empty;
    public int DocumentsRetrieved { get; set; }
}

/// <summary>
/// Source reference for citations
/// </summary>
public class SourceReference
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Document ingestion request for RAG
/// </summary>
public class RagDocumentRequest
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Batch ingestion request
/// </summary>
public class RagBatchIngestRequest
{
    public List<RagDocumentRequest> Documents { get; set; } = new();
    public bool GenerateEmbeddings { get; set; } = true;
}

/// <summary>
/// RAG document with embeddings
/// Interview Tip: Embeddings enable semantic search beyond keyword matching
/// </summary>
public class RagDocument
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public float[]? Embedding { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string>? Metadata { get; set; }
}
