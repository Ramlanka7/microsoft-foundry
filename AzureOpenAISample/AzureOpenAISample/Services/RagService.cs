using Azure;
using Azure.AI.OpenAI;
using AzureOpenAISample.Models;
using System.Text;

namespace AzureOpenAISample.Services;

/// <summary>
/// Implementation of RAG Service
/// 
/// Interview Key Points:
/// 1. RAG Pattern: Retrieve → Augment → Generate
/// 2. Combines search (Cognitive Search) with generation (OpenAI)
/// 3. Reduces hallucination by providing factual context
/// 4. Embeddings enable semantic similarity search
/// 5. Source citations for transparency and verification
/// 
/// Architecture:
/// User Query → Generate Embedding → Search Similar Docs → 
/// Build Context Prompt → LLM Generation → Return with Sources
/// </summary>
public class RagService : IRagService
{
    private readonly IAzureOpenAIService _openAIService;
    private readonly IAzureCognitiveSearchService _searchService;
    private readonly ILogger<RagService> _logger;
    
    public RagService(
        IAzureOpenAIService openAIService,
        IAzureCognitiveSearchService searchService,
        ILogger<RagService> logger)
    {
        _openAIService = openAIService;
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// RAG Query Implementation
    /// 
    /// Interview Talking Points:
    /// - Step 1: Search for relevant documents (retrieval)
    /// - Step 2: Build context from search results
    /// - Step 3: Create augmented prompt with context
    /// - Step 4: Generate answer using OpenAI (generation)
    /// - Step 5: Extract and return source references
    /// 
    /// Benefits over direct LLM:
    /// - Factually grounded in your documents
    /// - Always up-to-date (search current index)
    /// - Transparent sources for verification
    /// - Domain-specific without model fine-tuning
    /// </summary>
    public async Task<RagQueryResponse> QueryAsync(RagQueryRequest request)
    {
        try
        {
            _logger.LogInformation("Processing RAG query: {Query}", request.Query);

            // Step 1: Retrieve relevant documents
            var searchQuery = new SearchQuery
            {
                SearchText = request.Query,
                Top = request.MaxSearchResults
            };
            
            var searchResults = await _searchService.SearchAsync(searchQuery);
            
            if (!searchResults.Documents.Any())
            {
                _logger.LogWarning("No documents found for query: {Query}", request.Query);
                return new RagQueryResponse
                {
                    Answer = "I couldn't find any relevant information in the knowledge base to answer your question.",
                    SearchQuery = request.Query,
                    DocumentsRetrieved = 0
                };
            }

            // Step 2: Build context from retrieved documents
            var context = BuildContext(searchResults.Documents);
            
            // Step 3: Create augmented prompt
            var augmentedPrompt = BuildAugmentedPrompt(request.Query, context);
            
            // Step 4: Generate answer using OpenAI
            var chatRequest = new ChatRequest
            {
                Message = augmentedPrompt,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature
            };
            
            var chatResponse = await _openAIService.GetChatCompletionAsync(chatRequest);
            
            // Step 5: Build source references
            var sources = searchResults.Documents.Select((doc, index) => new SourceReference
            {
                DocumentId = doc.Id ?? $"doc-{index}",
                Title = doc.Title ?? "Untitled",
                Content = TruncateContent(doc.Content ?? "", 200),
                RelevanceScore = 1.0 - (index * 0.1) // Simple scoring
            }).ToList();
            
            _logger.LogInformation("RAG query completed. Retrieved {Count} documents, used {Tokens} tokens",
                searchResults.Documents.Count, chatResponse.TokensUsed);

            return new RagQueryResponse
            {
                Answer = chatResponse.Response,
                Sources = request.IncludeSourceReferences ? sources : new List<SourceReference>(),
                TokensUsed = chatResponse.TokensUsed,
                SearchQuery = request.Query,
                DocumentsRetrieved = searchResults.Documents.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG query failed");
            throw new ApplicationException($"RAG Query Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Build context from search results
    /// Interview Tip: Context is formatted to be clear and structured for the LLM
    /// </summary>
    private string BuildContext(List<SearchDocument> documents)
    {
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("Based on the following information from the knowledge base:\n");
        
        for (int i = 0; i < documents.Count; i++)
        {
            var doc = documents[i];
            contextBuilder.AppendLine($"[Source {i + 1}] {doc.Title}");
            contextBuilder.AppendLine(doc.Content);
            contextBuilder.AppendLine();
        }
        
        return contextBuilder.ToString();
    }

    /// <summary>
    /// Build augmented prompt for RAG
    /// 
    /// Interview Talking Points:
    /// - System message sets behavior (factual, cite sources)
    /// - Context is injected before the user question
    /// - Instructions guide the model to use context
    /// - Prevents hallucination by constraining to provided context
    /// </summary>
    private string BuildAugmentedPrompt(string query, string context)
    {
        return $@"{context}

Instructions:
- Answer the question based ONLY on the information provided above
- If the answer is not in the provided context, say ""I don't have enough information to answer that question""
- Be specific and cite which source(s) you used
- Keep your answer concise and relevant

Question: {query}

Answer:";
    }

    /// <summary>
    /// Ingest document with embeddings
    /// 
    /// Interview Talking Points:
    /// - Embeddings are vector representations (1536 dimensions)
    /// - Generated using OpenAI text-embedding-ada-002 model
    /// - Enables semantic similarity search
    /// - One-time cost at ingestion, faster searches later
    /// </summary>
    public async Task<bool> IngestDocumentAsync(RagDocumentRequest document)
    {
        try
        {
            _logger.LogInformation("Ingesting document: {Id}", document.Id);

            // Generate embedding for the document content
            var embedding = await _openAIService.GetEmbeddingsAsync(document.Content);
            
            // Create search document with embedding
            var searchDoc = new SearchDocument
            {
                Id = document.Id,
                Title = document.Title,
                Content = document.Content,
                Category = document.Category,
                CreatedDate = DateTime.UtcNow,
                Tags = document.Metadata?.Values.ToArray()
            };
            
            // Index the document
            var indexed = await _searchService.IndexDocumentAsync(searchDoc);
            
            _logger.LogInformation("Document {Id} ingested successfully", document.Id);
            return indexed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest document {Id}", document.Id);
            throw new ApplicationException($"Document Ingestion Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Batch ingest documents
    /// Interview Tip: Batching is more efficient and cost-effective
    /// </summary>
    public async Task<int> IngestDocumentsBatchAsync(List<RagDocumentRequest> documents)
    {
        try
        {
            _logger.LogInformation("Batch ingesting {Count} documents", documents.Count);

            var searchDocuments = new List<SearchDocument>();
            
            foreach (var doc in documents)
            {
                // Note: In production, you'd batch embedding generation too
                // For now, we'll index without embeddings for performance
                searchDocuments.Add(new SearchDocument
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    Content = doc.Content,
                    Category = doc.Category,
                    CreatedDate = DateTime.UtcNow,
                    Tags = doc.Metadata?.Values.ToArray()
                });
            }
            
            await _searchService.IndexDocumentsAsync(searchDocuments);
            
            _logger.LogInformation("Batch ingestion completed: {Count} documents", documents.Count);
            return documents.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch ingestion failed");
            throw new ApplicationException($"Batch Ingestion Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Search similar documents using semantic search
    /// Interview Tip: This is pure vector search based on embedding similarity
    /// </summary>
    public async Task<List<SourceReference>> SearchSimilarDocumentsAsync(string query, int topK = 5)
    {
        try
        {
            _logger.LogInformation("Searching similar documents for: {Query}", query);

            var searchQuery = new SearchQuery
            {
                SearchText = query,
                Top = topK
            };
            
            var results = await _searchService.SearchAsync(searchQuery);
            
            return results.Documents.Select((doc, index) => new SourceReference
            {
                DocumentId = doc.Id ?? $"doc-{index}",
                Title = doc.Title ?? "Untitled",
                Content = TruncateContent(doc.Content ?? "", 300),
                RelevanceScore = 1.0 - (index * 0.1)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Similar document search failed");
            throw new ApplicationException($"Search Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Helper to truncate content for display
    /// </summary>
    private string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;
        
        return content.Substring(0, maxLength) + "...";
    }
}
