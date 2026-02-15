using Microsoft.AspNetCore.Mvc;
using AzureOpenAISample.Models;
using AzureOpenAISample.Services;

namespace AzureOpenAISample.Controllers;

/// <summary>
/// Vector Search Controller - Semantic search with embeddings
/// 
/// INTERVIEW PREP:
/// Q: "What's the difference between vector search and traditional search?"
/// A: "Traditional search (BM25) matches keywords/terms. Vector search converts text 
///     to embeddings (numerical vectors) and finds semantically similar content using 
///     cosine similarity. For example, 'car' and 'automobile' are different keywords 
///     but similar vectors."
/// 
/// Q: "When would you use vector search?"
/// A: "When semantic meaning matters more than exact keywords: semantic search, 
///     recommendation systems, duplicate detection, multilingual search, Q&A systems."
/// 
/// Q: "What are the tradeoffs?"
/// A: "Vector search: better semantic understanding, but costly (embeddings required).
///     Traditional: fast, cheap, but misses semantic matches.
///     Hybrid: best of both!"
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VectorSearchController : ControllerBase
{
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ILogger<VectorSearchController> _logger;

    public VectorSearchController(
        IVectorSearchService vectorSearchService,
        ILogger<VectorSearchController> logger)
    {
        _vectorSearchService = vectorSearchService;
        _logger = logger;
    }

    /// <summary>
    /// Create or update the vector search index
    /// IMPORTANT: Run this first before adding documents!
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("create-index")]
    public async Task<IActionResult> CreateIndex()
    {
        try
        {
            await _vectorSearchService.CreateOrUpdateIndexAsync();
            return Ok(new { message = "Vector search index created/updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create index");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Pure vector search - semantic similarity only
    /// Best for: conceptual search, finding similar content
    /// </summary>
    /// <param name="request">Vector search request</param>
    /// <returns>Semantically similar documents</returns>
    [HttpPost("vector-search")]
    public async Task<IActionResult> VectorSearch([FromBody] VectorSearchRequest request)
    {
        try
        {
            request.Mode = SearchMode.Vector;
            var results = await _vectorSearchService.VectorSearchAsync(request);
            
            return Ok(new
            {
                mode = "vector",
                query = request.Query,
                totalResults = results.TotalCount,
                results = results.Results.Select(r => new
                {
                    score = Math.Round(r.Score, 4),
                    title = r.Document.Title,
                    content = r.Document.Content.Length > 200 
                        ? r.Document.Content.Substring(0, 200) + "..." 
                        : r.Document.Content,
                    category = r.Document.Category,
                    tags = r.Document.Tags
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Hybrid search - combines keyword + semantic search
    /// RECOMMENDED: This gives the best results in production!
    /// 
    /// INTERVIEW GOLD: "Hybrid search uses Reciprocal Rank Fusion (RRF) to combine
    /// BM25 keyword scores and vector similarity scores. It catches both exact matches
    /// and semantic matches."
    /// </summary>
    /// <param name="request">Hybrid search request</param>
    /// <returns>Best matching documents (keyword + semantic)</returns>
    [HttpPost("hybrid-search")]
    public async Task<IActionResult> HybridSearch([FromBody] VectorSearchRequest request)
    {
        try
        {
            request.Mode = SearchMode.Hybrid;
            var results = await _vectorSearchService.HybridSearchAsync(request);
            
            return Ok(new
            {
                mode = "hybrid",
                query = request.Query,
                totalResults = results.TotalCount,
                explanation = "Combines BM25 keyword matching + vector semantic similarity using RRF",
                results = results.Results.Select(r => new
                {
                    score = Math.Round(r.Score, 4),
                    title = r.Document.Title,
                    content = r.Document.Content.Length > 200 
                        ? r.Document.Content.Substring(0, 200) + "..." 
                        : r.Document.Content,
                    category = r.Document.Category,
                    tags = r.Document.Tags
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hybrid search failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Compare search modes side-by-side
    /// Shows difference between vector and hybrid search
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="top">Number of results per mode</param>
    /// <returns>Comparison of search results</returns>
    [HttpGet("compare")]
    public async Task<IActionResult> CompareSearchModes(
        [FromQuery] string query = "machine learning",
        [FromQuery] int top = 5)
    {
        try
        {
            var request = new VectorSearchRequest { Query = query, Top = top };

            // Execute both searches in parallel
            var vectorTask = _vectorSearchService.VectorSearchAsync(request);
            var hybridTask = _vectorSearchService.HybridSearchAsync(request);

            await Task.WhenAll(vectorTask, hybridTask);

            var vectorResults = await vectorTask;
            var hybridResults = await hybridTask;

            return Ok(new
            {
                query,
                comparison = new
                {
                    vectorSearch = new
                    {
                        description = "Semantic similarity only (uses embeddings)",
                        count = vectorResults.TotalCount,
                        results = vectorResults.Results.Take(3).Select(r => new
                        {
                            score = Math.Round(r.Score, 4),
                            title = r.Document.Title
                        })
                    },
                    hybridSearch = new
                    {
                        description = "Keyword (BM25) + Semantic (RRF fusion)",
                        count = hybridResults.TotalCount,
                        results = hybridResults.Results.Take(3).Select(r => new
                        {
                            score = Math.Round(r.Score, 4),
                            title = r.Document.Title
                        })
                    }
                },
                recommendation = "Hybrid search typically gives best results!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search comparison failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add a document with auto-generated embeddings
    /// </summary>
    /// <param name="document">Document to add</param>
    /// <returns>Document ID</returns>
    [HttpPost("add-document")]
    public async Task<IActionResult> AddDocument([FromBody] VectorDocumentRequest document)
    {
        try
        {
            var documentId = await _vectorSearchService.AddDocumentAsync(document);
            return Ok(new 
            { 
                message = "Document added with embeddings",
                documentId,
                embeddingDimensions = 1536,
                model = "text-embedding-ada-002"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Batch add documents (more efficient than adding one at a time)
    /// </summary>
    /// <param name="documents">Documents to add</param>
    /// <returns>Document IDs</returns>
    [HttpPost("add-documents-batch")]
    public async Task<IActionResult> AddDocumentsBatch([FromBody] List<VectorDocumentRequest> documents)
    {
        try
        {
            var documentIds = await _vectorSearchService.AddDocumentsBatchAsync(documents);
            return Ok(new 
            { 
                message = $"Batch added {documents.Count} documents with embeddings",
                documentIds,
                count = documents.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch add documents");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Seed sample data for testing vector search
    /// Creates documents about Azure, AI, and .NET topics
    /// </summary>
    /// <returns>Created document IDs</returns>
    [HttpPost("seed-data")]
    public async Task<IActionResult> SeedSampleData()
    {
        try
        {
            var sampleDocs = new List<VectorDocumentRequest>
            {
                new()
                {
                    Title = "Introduction to Vector Search",
                    Content = "Vector search, also known as semantic search, uses machine learning embeddings to find similar content. Unlike traditional keyword search, vector search understands context and meaning. It's particularly useful for finding documents that are conceptually similar even if they use different terminology.",
                    Category = "Search",
                    Tags = new[] { "vector-search", "embeddings", "AI" }
                },
                new()
                {
                    Title = "Azure Cognitive Search Overview",
                    Content = "Azure Cognitive Search is a cloud search service that provides infrastructure, APIs, and tools for building rich search experiences. It supports full-text search, vector search, and hybrid search patterns. The service integrates with Azure OpenAI for semantic ranking capabilities.",
                    Category = "Azure",
                    Tags = new[] { "azure", "search", "cognitive-search" }
                },
                new()
                {
                    Title = "Understanding Embeddings",
                    Content = "Embeddings are numerical representations of text in high-dimensional space. Words or phrases with similar meanings have similar embeddings. Models like text-embedding-ada-002 from OpenAI convert text into 1536-dimensional vectors that capture semantic relationships.",
                    Category = "AI",
                    Tags = new[] { "embeddings", "NLP", "machine-learning" }
                },
                new()
                {
                    Title = "RAG Architecture Patterns",
                    Content = "Retrieval Augmented Generation (RAG) combines search with language models. The pattern involves three steps: retrieve relevant documents using vector search, augment the prompt with retrieved context, and generate responses grounded in facts. This prevents hallucination.",
                    Category = "Architecture",
                    Tags = new[] { "RAG", "LLM", "architecture" }
                },
                new()
                {
                    Title = "Hybrid Search Explained",
                    Content = "Hybrid search combines traditional BM25 keyword search with vector semantic search. BM25 catches exact terms and proper nouns while vectors handle synonyms and paraphrases. Azure Cognitive Search uses Reciprocal Rank Fusion (RRF) to merge results intelligently.",
                    Category = "Search",
                    Tags = new[] { "hybrid-search", "BM25", "RRF" }
                },
                new()
                {
                    Title = ".NET 8 Performance Improvements",
                    Content = "ASP.NET Core 8 introduces significant performance enhancements including improved JSON serialization, faster minimal APIs, and better async performance. The runtime includes PGO (Profile Guided Optimization) improvements and SIMD vectorization.",
                    Category = "DotNet",
                    Tags = new[] { "dotnet", "performance", "aspnetcore" }
                },
                new()
                {
                    Title = "HNSW Algorithm",
                    Content = "Hierarchical Navigable Small World (HNSW) is an approximate nearest neighbor algorithm used for fast vector search. It builds a multi-layer graph structure that enables logarithmic search complexity. Parameters like M and efSearch control the accuracy-speed tradeoff.",
                    Category = "Algorithms",
                    Tags = new[] { "HNSW", "ANN", "algorithms" }
                },
                new()
                {
                    Title = "Cosine Similarity",
                    Content = "Cosine similarity measures the angle between two vectors in multi-dimensional space. It ranges from -1 to 1, where 1 means identical direction (very similar), 0 means orthogonal (unrelated), and -1 means opposite. It's ideal for normalized embeddings like those from OpenAI.",
                    Category = "Mathematics",
                    Tags = new[] { "similarity", "vectors", "mathematics" }
                }
            };

            var documentIds = await _vectorSearchService.AddDocumentsBatchAsync(sampleDocs);
            
            return Ok(new
            {
                message = "Sample data seeded successfully",
                documentsCreated = sampleDocs.Count,
                documentIds,
                categories = sampleDocs.Select(d => d.Category).Distinct(),
                testQuery = "Try searching: 'What is semantic search?' or 'How does RAG work?'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed sample data");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(string id)
    {
        try
        {
            var document = await _vectorSearchService.GetDocumentAsync(id);
            if (document == null)
                return NotFound(new { error = "Document not found" });

            return Ok(new
            {
                document.Id,
                document.Title,
                document.Content,
                document.Category,
                document.Tags,
                document.CreatedDate,
                document.SourceUrl,
                vectorDimensions = document.ContentVector?.Length ?? 0,
                hasEmbedding = document.ContentVector != null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete document
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(string id)
    {
        try
        {
            await _vectorSearchService.DeleteDocumentAsync(id);
            return Ok(new { message = "Document deleted", documentId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Demo endpoint with examples
    /// </summary>
    [HttpGet("demo")]
    public IActionResult Demo()
    {
        return Ok(new
        {
            title = "Vector Search API Demo",
            description = "Semantic search using Azure Cognitive Search + OpenAI embeddings",
            
            setup = new
            {
                step1 = "POST /api/VectorSearch/create-index (create index schema)",
                step2 = "POST /api/VectorSearch/seed-data (add sample documents)",
                step3 = "POST /api/VectorSearch/hybrid-search (search!)"
            },

            searchModes = new
            {
                vector = new
                {
                    description = "Pure semantic search using embeddings",
                    pros = "Finds semantically similar content, handles synonyms",
                    cons = "May miss exact keyword matches",
                    endpoint = "POST /api/VectorSearch/vector-search"
                },
                hybrid = new
                {
                    description = "Combines BM25 keyword + vector semantic search",
                    pros = "Best of both worlds - keywords + semantics",
                    cons = "Slightly higher latency",
                    endpoint = "POST /api/VectorSearch/hybrid-search",
                    recommended = true
                }
            },

            examples = new[]
            {
                new
                {
                    scenario = "Semantic understanding",
                    query = "What is semantic search?",
                    finds = "Documents about vector search, embeddings (synonyms)"
                },
                new
                {
                    scenario = "Concept matching",
                    query = "How to prevent LLM hallucination?",
                    finds = "Documents about RAG, grounding (related concepts)"
                },
                new
                {
                    scenario = "Technical terms",
                    query = "HNSW algorithm",
                    finds = "Exact technical term + related vector search content"
                }
            },

            interviewTips = new[]
            {
                "Vector search converts text to embeddings (1536-dim for ada-002)",
                "HNSW = Hierarchical Navigable Small World (fast ANN algorithm)",
                "Cosine similarity measures angle between vectors (0-1 range)",
                "Hybrid search uses RRF (Reciprocal Rank Fusion) to merge results",
                "Production systems typically use hybrid, not pure vector"
            }
        });
    }
}
