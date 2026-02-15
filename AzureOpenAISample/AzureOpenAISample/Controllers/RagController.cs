using AzureOpenAISample.Models;
using AzureOpenAISample.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureOpenAISample.Controllers;

/// <summary>
/// RAG (Retrieval Augmented Generation) Controller
/// 
/// Interview Preparation - RAG System:
/// 
/// Q: What is RAG?
/// A: Retrieval Augmented Generation - combines document retrieval with LLM generation
///    to provide factually grounded, domain-specific answers without model fine-tuning.
/// 
/// Q: Why use RAG over fine-tuning?
/// A: 
/// - Cost-effective (no model training)
/// - Always up-to-date (just update documents)
/// - Transparent (can cite sources)
/// - Lower latency (no model deployment)
/// - Works with any model
/// 
/// Q: RAG vs just using LLM?
/// A: RAG prevents hallucination by grounding answers in real documents,
///    provides sources for verification, and enables domain expertise
///    without being limited by model's training data cutoff.
/// 
/// Q: How does it work?
/// A: 1. User asks question
///    2. Search retrieves relevant documents (keyword + semantic)
///    3. Documents are injected into LLM prompt as context
///    4. LLM generates answer based on provided context
///    5. Return answer with source citations
/// 
/// Q: What are the challenges?
/// A: 
/// - Context window limits (must fit docs in prompt)
/// - Search quality (bad retrieval = bad answers)
/// - Embedding costs (for semantic search)
/// - Keeping index up-to-date
/// - Chunking strategy for large documents
/// 
/// Q: Improvements for production?
/// A:
/// - Hybrid search (keyword + vector)
/// - Re-ranking retrieved documents
/// - Chunking large documents
/// - Query expansion/rewriting
/// - Caching frequent queries
/// - Streaming responses
/// - Conversation history
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RagController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly ILogger<RagController> _logger;

    public RagController(
        IRagService ragService,
        ILogger<RagController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// POST: api/Rag/query
    /// Query the RAG system
    /// 
    /// Example Request:
    /// {
    ///   "query": "What are the benefits of Azure OpenAI?",
    ///   "maxSearchResults": 3,
    ///   "maxTokens": 500,
    ///   "temperature": 0.7,
    ///   "includeSourceReferences": true
    /// }
    /// 
    /// Interview Tip: Temperature controls randomness
    /// - 0.0 = Deterministic, factual
    /// - 0.7 = Balanced
    /// - 1.0+ = Creative, varied
    /// For RAG, lower temperature (0.3-0.5) is better for factual accuracy
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] RagQueryRequest request)
    {
        if (string.IsNullOrEmpty(request.Query))
        {
            return BadRequest("Query is required");
        }

        try
        {
            _logger.LogInformation("Processing RAG query: {Query}", request.Query);
            var response = await _ragService.QueryAsync(request);
            
            return Ok(new
            {
                success = true,
                data = response,
                metadata = new
                {
                    query = request.Query,
                    documentsRetrieved = response.DocumentsRetrieved,
                    tokensUsed = response.TokensUsed,
                    sourcesReturned = response.Sources.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG query failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/Rag/ingest
    /// Ingest a single document into the RAG system
    /// 
    /// Example Request:
    /// {
    ///   "id": "doc-1",
    ///   "title": "Azure OpenAI Overview",
    ///   "content": "Azure OpenAI Service provides REST API access to OpenAI's powerful language models...",
    ///   "category": "Azure",
    ///   "metadata": {
    ///     "author": "Microsoft",
    ///     "date": "2024-01-15"
    ///   }
    /// }
    /// 
    /// Interview Tip: Embeddings are generated at ingestion time
    /// This is more efficient than generating at query time
    /// </summary>
    [HttpPost("ingest")]
    public async Task<IActionResult> IngestDocument([FromBody] RagDocumentRequest document)
    {
        if (string.IsNullOrEmpty(document.Id) || string.IsNullOrEmpty(document.Content))
        {
            return BadRequest("Document ID and Content are required");
        }

        try
        {
            var success = await _ragService.IngestDocumentAsync(document);
            return Ok(new
            {
                success = success,
                message = $"Document '{document.Id}' ingested successfully",
                documentId = document.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document ingestion failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/Rag/ingest-batch
    /// Batch ingest multiple documents
    /// 
    /// Interview Tip: Batching reduces API calls and is more cost-effective
    /// For large document sets, implement chunking (split into 1000-doc batches)
    /// </summary>
    [HttpPost("ingest-batch")]
    public async Task<IActionResult> IngestDocumentsBatch([FromBody] List<RagDocumentRequest> documents)
    {
        if (!documents.Any())
        {
            return BadRequest("At least one document is required");
        }

        try
        {
            var count = await _ragService.IngestDocumentsBatchAsync(documents);
            return Ok(new
            {
                success = true,
                message = $"{count} documents ingested successfully",
                count = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch ingestion failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/Rag/seed-knowledge-base
    /// Create sample RAG documents for testing
    /// 
    /// Interview Talking Points:
    /// - Real RAG systems have hundreds to millions of documents
    /// - Documents should be chunked (500-1000 tokens per chunk)
    /// - Overlap chunks by 10-20% for context continuity
    /// - Include metadata for filtering
    /// </summary>
    [HttpPost("seed-knowledge-base")]
    public async Task<IActionResult> SeedKnowledgeBase()
    {
        try
        {
            var sampleDocuments = new List<RagDocumentRequest>
            {
                new RagDocumentRequest
                {
                    Id = "rag-doc-1",
                    Title = "Azure OpenAI Service Overview",
                    Content = @"Azure OpenAI Service provides REST API access to OpenAI's powerful language models including GPT-4, GPT-3.5-Turbo, and Embeddings models. The service combines the robust capabilities of OpenAI models with the security, compliance, and regional availability of Azure. Key features include:

1. Enterprise-grade security with managed identity support
2. Private networking with Azure Virtual Networks
3. Content filtering and responsible AI features
4. Global availability across multiple Azure regions
5. Pay-as-you-go pricing based on token usage

Azure OpenAI is ideal for building intelligent applications including chatbots, content generation, code completion, semantic search, and data analysis. The service includes comprehensive monitoring and logging through Azure Monitor and Application Insights.",
                    Category = "Azure AI",
                    Metadata = new Dictionary<string, string>
                    {
                        ["Source"] = "Azure Documentation",
                        ["LastUpdated"] = "2024-01-15"
                    }
                },
                new RagDocumentRequest
                {
                    Id = "rag-doc-2",
                    Title = "What is RAG (Retrieval Augmented Generation)?",
                    Content = @"Retrieval Augmented Generation (RAG) is a technique that enhances Large Language Models (LLMs) by combining them with information retrieval systems. Instead of relying solely on the model's training data, RAG retrieves relevant documents from a knowledge base and uses them as context for generation.

The RAG process works in three steps:
1. Retrieval: Search a document store for relevant information
2. Augmentation: Inject retrieved documents into the LLM prompt
3. Generation: LLM generates an answer grounded in the provided context

Benefits of RAG:
- Reduces hallucination by grounding answers in facts
- Enables domain expertise without fine-tuning
- Always up-to-date (just update document store)
- Transparent with source citations
- More cost-effective than model training

RAG is particularly useful for enterprise Q&A systems, customer support, documentation search, and knowledge management applications.",
                    Category = "AI Concepts",
                    Metadata = new Dictionary<string, string>
                    {
                        ["Source"] = "AI Research",
                        ["LastUpdated"] = "2024-02-01"
                    }
                },
                new RagDocumentRequest
                {
                    Id = "rag-doc-3",
                    Title = "Azure Cognitive Search for RAG",
                    Content = @"Azure Cognitive Search is an ideal retrieval system for RAG applications. It provides:

1. Full-text search with BM25 ranking algorithm for keyword matching
2. Vector search for semantic similarity using embeddings
3. Hybrid search combining both approaches
4. Faceting and filtering for refined searches
5. Scalable indexing for millions of documents

For RAG implementations, Cognitive Search enables:
- Fast document retrieval (sub-second queries)
- Semantic search using OpenAI embeddings
- Re-ranking capabilities for improved relevance
- Distributed indexing across partitions
- Integration with Azure OpenAI for end-to-end RAG

Best practices include chunking large documents into 500-1000 token segments, generating embeddings for semantic search, using metadata for filtering, and implementing caching for frequent queries.",
                    Category = "Azure Search",
                    Metadata = new Dictionary<string, string>
                    {
                        ["Source"] = "Azure Documentation",
                        ["LastUpdated"] = "2024-01-20"
                    }
                },
                new RagDocumentRequest
                {
                    Id = "rag-doc-4",
                    Title = "Managed Identity for Secure RAG",
                    Content = @"Managed Identity is crucial for building secure RAG systems in Azure. It eliminates the need for storing credentials in your code or configuration.

For RAG applications, Managed Identity provides:
- Secure authentication to Azure OpenAI (no API keys)
- Secure access to Cognitive Search (no admin keys)
- Secure blob storage access for document ingestion
- Automated credential rotation
- Azure RBAC for fine-grained permissions

Implementation steps:
1. Enable System-Assigned Managed Identity on your App Service
2. Assign 'Cognitive Services OpenAI User' role for Azure OpenAI access
3. Assign 'Search Index Data Contributor' role for Cognitive Search
4. Assign 'Storage Blob Data Contributor' for blob storage
5. Use DefaultAzureCredential in your code

This approach follows zero-trust security principles and is recommended for all production RAG deployments.",
                    Category = "Security",
                    Metadata = new Dictionary<string, string>
                    {
                        ["Source"] = "Security Best Practices",
                        ["LastUpdated"] = "2024-02-05"
                    }
                },
                new RagDocumentRequest
                {
                    Id = "rag-doc-5",
                    Title = ".NET 8 for Building RAG APIs",
                    Content = @".NET 8 is an excellent choice for building RAG (Retrieval Augmented Generation) APIs. As the latest Long-Term Support (LTS) release, it offers:

Performance benefits:
- 20-30% faster than .NET 6
- Improved async/await performance
- Optimized JSON serialization
- Native AOT compilation support

Features for RAG systems:
- Strong typing with Azure SDK for .NET
- Built-in dependency injection
- Comprehensive logging integration
- Application Insights for monitoring
- Minimal APIs for lightweight endpoints
- Excellent async/await support for I/O operations

For RAG APIs, use:
- Singleton lifetime for Azure SDK clients (thread-safe)
- ILogger for structured logging
- IOptions pattern for configuration
- Async/await throughout for non-blocking operations
- Proper error handling with try-catch
- Response caching for frequent queries

The Azure SDK for .NET provides first-class support for OpenAI, Cognitive Search, and Blob Storage, making it ideal for RAG implementations.",
                    Category = ".NET Development",
                    Metadata = new Dictionary<string, string>
                    {
                        ["Source"] = "Microsoft Docs",
                        ["LastUpdated"] = "2024-01-10"
                    }
                }
            };

            var count = await _ragService.IngestDocumentsBatchAsync(sampleDocuments);
            
            return Ok(new
            {
                success = true,
                message = $"Knowledge base seeded with {count} documents",
                count = count,
                documents = sampleDocuments.Select(d => new { d.Id, d.Title, d.Category }),
                tip = "Try asking: 'What is RAG?' or 'How does Managed Identity work?' or 'Why use Azure OpenAI?'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed knowledge base");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/Rag/search-similar
    /// Search for similar documents without generation
    /// Useful for testing retrieval quality
    /// 
    /// Interview Tip: Good retrieval is critical for RAG success
    /// Test retrieval separately to ensure quality before full RAG pipeline
    /// </summary>
    [HttpPost("search-similar")]
    public async Task<IActionResult> SearchSimilar([FromBody] SearchSimilarRequest request)
    {
        if (string.IsNullOrEmpty(request.Query))
        {
            return BadRequest("Query is required");
        }

        try
        {
            var results = await _ragService.SearchSimilarDocumentsAsync(request.Query, request.TopK);
            return Ok(new
            {
                success = true,
                query = request.Query,
                resultsCount = results.Count,
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Similar search failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET: api/Rag/demo
    /// Interactive demo showcasing RAG capabilities
    /// </summary>
    [HttpGet("demo")]
    public IActionResult Demo()
    {
        return Ok(new
        {
            title = "RAG API Demo",
            description = "Retrieval Augmented Generation - Intelligent Q&A System",
            features = new[]
            {
                "Document retrieval with Azure Cognitive Search",
                "Answer generation with Azure OpenAI",
                "Source citations for transparency",
                "Semantic search with embeddings",
                "Batch document ingestion"
            },
            quickStart = new
            {
                step1 = "POST /api/Rag/seed-knowledge-base - Create sample documents",
                step2 = "POST /api/Rag/query - Ask questions about the content",
                step3 = "Review answers with source references"
            },
            sampleQueries = new[]
            {
                "What is RAG and how does it work?",
                "What are the benefits of Azure OpenAI?",
                "How does Managed Identity improve security?",
                "Why use .NET 8 for RAG systems?",
                "How do I use Azure Cognitive Search with RAG?"
            },
            interviewTopics = new[]
            {
                "RAG architecture and benefits",
                "Retrieval strategies (keyword vs semantic)",
                "Context window management",
                "Chunking strategies for large documents",
                "Caching and performance optimization",
                "Hallucination prevention",
                "Source attribution and citations",
                "Hybrid search approaches"
            }
        });
    }
}

/// <summary>
/// Search similar documents request
/// </summary>
public class SearchSimilarRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
}
