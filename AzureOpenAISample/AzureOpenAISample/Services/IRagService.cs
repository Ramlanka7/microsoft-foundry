using AzureOpenAISample.Models;

namespace AzureOpenAISample.Services;

/// <summary>
/// RAG (Retrieval Augmented Generation) Service
/// 
/// Interview Topics:
/// 1. What is RAG? Combines retrieval (search) with generation (LLM)
/// 2. Overcomes LLM hallucination by grounding in real documents
/// 3. Enables domain-specific knowledge without fine-tuning
/// 4. Two-stage process: Retrieve relevant docs → Generate answer with context
/// 5. Hybrid search: Keyword + Semantic (vector) for best results
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Query using RAG pattern
    /// Steps: 1) Search for relevant docs, 2) Generate answer with context
    /// </summary>
    Task<RagQueryResponse> QueryAsync(RagQueryRequest request);
    
    /// <summary>
    /// Ingest a single document with embeddings
    /// Interview Tip: Embeddings are generated using Azure OpenAI text-embedding-ada-002
    /// </summary>
    Task<bool> IngestDocumentAsync(RagDocumentRequest document);
    
    /// <summary>
    /// Batch ingest multiple documents
    /// Interview Tip: More efficient than individual ingestion
    /// </summary>
    Task<int> IngestDocumentsBatchAsync(List<RagDocumentRequest> documents);
    
    /// <summary>
    /// Search for similar documents using vector search
    /// Interview Tip: Compares query embedding with stored document embeddings
    /// </summary>
    Task<List<SourceReference>> SearchSimilarDocumentsAsync(string query, int topK = 5);
}
