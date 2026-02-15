# 🔍 Azure Cognitive Search Vector Setup Guide

## Table of Contents
1. [What is Vector Search?](#what-is-vector-search)
2. [Architecture Overview](#architecture-overview)
3. [Index Setup](#index-setup)
4. [Search Modes](#search-modes)
5. [Code Examples](#code-examples)
6. [Interview Q&A](#interview-qa)
7. [Performance Tuning](#performance-tuning)

---

## What is Vector Search?

### Traditional Search (BM25) vs Vector Search

**Traditional Keyword Search (BM25):**
- Matches exact keywords and terms
- Example: Query "car" only finds documents containing "car"
- Misses synonyms: "automobile", "vehicle"
- Fast and cheap

**Vector Search (Semantic):**
- Converts text to embeddings (numerical vectors)
- Finds semantically similar content
- Example: Query "car" finds "automobile", "vehicle", "sedan"
- Understands meaning, not just words
- Requires embedding model (cost + latency)

### Key Concepts

```
Text → Embedding Model → Vector (1536 dimensions for ada-002)
"machine learning" → [0.234, -0.567, 0.891, ..., 0.123]
"AI algorithms"    → [0.221, -0.543, 0.903, ..., 0.134]  ← Similar!
```

**Cosine Similarity:**
- Measures angle between two vectors
- Range: -1 to 1 (1 = identical, 0 = unrelated, -1 = opposite)
- Formula: `similarity = (A · B) / (||A|| × ||B||)`

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Vector Search Architecture                    │
└─────────────────────────────────────────────────────────────────┘

1. INDEXING PHASE:
   ┌──────────────┐      ┌──────────────────┐      ┌──────────────────┐
   │   Document   │──────│  OpenAI Embed    │──────│ Cognitive Search │
   │  "ML is..."  │      │  text-ada-002    │      │  Store Doc +     │
   │              │      │  [0.2, -0.5,...] │      │  Vector(1536D)   │
   └──────────────┘      └──────────────────┘      └──────────────────┘

2. SEARCH PHASE:
   ┌──────────────┐      ┌──────────────────┐
   │    Query     │──────│  OpenAI Embed    │
   │ "What is AI?"│      │  [0.23,-0.51,...]│
   └──────────────┘      └──────────────────┘
                                │
                                ▼
                    ┌─────────────────────────┐
                    │   Cognitive Search      │
                    │   HNSW Algorithm        │
                    │   Find K-Nearest        │
                    │   (Cosine Similarity)   │
                    └─────────────────────────┘
                                │
                                ▼
                    ┌─────────────────────────┐
                    │   Top K Results         │
                    │   Score: 0.89 ⭐        │
                    │   Score: 0.85           │
                    │   Score: 0.82           │
                    └─────────────────────────┘
```

---

## Index Setup

### Step 1: Define Index Schema

```csharp
var vectorSearch = new VectorSearch();

// HNSW Algorithm Configuration
vectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration("hnsw-config")
{
    Parameters = new HnswParameters
    {
        M = 4,                    // Bi-directional links per node (4-10)
        EfConstruction = 400,     // Build quality (default 400)
        EfSearch = 500,           // Search quality (default 500)
        Metric = VectorSearchAlgorithmMetric.Cosine  // For text embeddings
    }
});

// Vector Profile (links algorithm to field)
vectorSearch.Profiles.Add(
    new VectorSearchProfile("my-vector-profile", "hnsw-config")
);

// Create Index
var index = new SearchIndex("vector-index")
{
    Fields =
    {
        new SimpleField("Id", SearchFieldDataType.String) { IsKey = true },
        new SearchableField("Title"),
        new SearchableField("Content"),
        
        // VECTOR FIELD (Critical!)
        new SearchField("ContentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
        {
            IsSearchable = true,
            VectorSearchDimensions = 1536,  // text-embedding-ada-002
            VectorSearchProfileName = "my-vector-profile"
        }
    },
    VectorSearch = vectorSearch
};

await indexClient.CreateOrUpdateIndexAsync(index);
```

### Step 2: Understanding HNSW Parameters

| Parameter | Description | Default | Tradeoff |
|-----------|-------------|---------|----------|
| **M** | Max bi-directional links per node | 4 | ↑ = Better recall, ↓ Speed, ↑ Memory |
| **efConstruction** | Build-time candidate list size | 400 | ↑ = Better quality, ↓ Build speed |
| **efSearch** | Search-time candidate list size | 500 | ↑ = Better recall, ↓ Query speed |
| **Metric** | Distance metric | Cosine | Cosine for normalized embeddings |

**Interview Tip:** "HNSW builds a multi-layer graph structure. Higher layers are sparser for fast navigation, lower layers are denser for accuracy. It gives logarithmic search complexity."

### Step 3: Embedding Dimensions by Model

| Model | Dimensions | Max Input Tokens | Cost (per 1M tokens) |
|-------|------------|------------------|----------------------|
| text-embedding-ada-002 | 1536 | 8,191 | $0.10 |
| text-embedding-3-small | 1536 | 8,191 | $0.02 |
| text-embedding-3-large | 3072 | 8,191 | $0.13 |

⚠️ **CRITICAL:** Index dimensions must match embedding model!

---

## Search Modes

### 1. Pure Vector Search

**When to Use:**
- Semantic/conceptual search
- Finding similar content
- Multilingual search (embeddings are cross-lingual)
- Recommendation systems

**Pros:**
- Understands meaning, not just keywords
- Handles synonyms, paraphrases
- Works across languages

**Cons:**
- Misses exact keyword matches
- Expensive (embedding generation)
- Slight latency overhead

**Code:**
```csharp
var queryEmbedding = await openAIService.GenerateEmbeddingsAsync(query);

var searchOptions = new SearchOptions
{
    VectorSearch = new()
    {
        Queries = { new VectorizedQuery(queryEmbedding.ToArray()) 
        { 
            KNearestNeighborsCount = 10,
            Fields = { "ContentVector" } 
        } }
    },
    Size = 5
};

var results = await searchClient.SearchAsync<Doc>(null, searchOptions);
```

### 2. Hybrid Search (RECOMMENDED!)

**When to Use:**
- Production systems (best accuracy)
- When both keywords and semantics matter
- General-purpose search

**How It Works:**
1. **BM25 Search:** Finds keyword matches
2. **Vector Search:** Finds semantic matches
3. **Reciprocal Rank Fusion (RRF):** Merges both result sets

**RRF Formula:**
```
RRF_score = Σ [ 1 / (k + rank_i) ]
```
Where:
- k = constant (typically 60)
- rank_i = position in result set i

**Pros:**
- Best of both worlds
- Catches exact terms AND semantic matches
- More robust to query phrasing

**Cons:**
- Slightly higher latency
- More complex scoring

**Code:**
```csharp
var queryEmbedding = await openAIService.GenerateEmbeddingsAsync(query);

var searchOptions = new SearchOptions
{
    // Traditional text search (BM25)
    SearchMode = Azure.Search.Documents.Models.SearchMode.All,
    QueryType = SearchQueryType.Simple,
    
    // Vector search
    VectorSearch = new()
    {
        Queries = { new VectorizedQuery(queryEmbedding.ToArray()) 
        { 
            KNearestNeighborsCount = 20,  // Get more candidates
            Fields = { "ContentVector" } 
        } }
    },
    Size = 5
};

// Both text query AND vector query
var results = await searchClient.SearchAsync<Doc>(query, searchOptions);
```

### 3. Traditional Text Search (BM25)

**When to Use:**
- Exact term matching required
- Technical documentation (exact API names, codes)
- Legal/compliance (exact phrases)
- Cost-sensitive scenarios

**Pros:**
- Fast and cheap
- Deterministic results
- No embedding overhead

**Cons:**
- Misses synonyms and paraphrases
- No semantic understanding
- Language-specific

---

## Code Examples

### Example 1: Index Documents with Embeddings

```csharp
public async Task<string> AddDocumentAsync(string title, string content)
{
    // 1. Generate embedding
    var embedding = await _openAIService.GenerateEmbeddingsAsync(content);
    
    // 2. Create document
    var document = new VectorSearchDocument
    {
        Id = Guid.NewGuid().ToString(),
        Title = title,
        Content = content,
        ContentVector = embedding.ToArray(),  // float[1536]
        CreatedDate = DateTime.UtcNow
    };
    
    // 3. Upload to index
    await _searchClient.IndexDocumentsAsync(
        IndexDocumentsBatch.Upload(new[] { document })
    );
    
    return document.Id;
}
```

### Example 2: Hybrid Search

```csharp
public async Task<List<SearchResult>> HybridSearchAsync(string query)
{
    // 1. Generate query embedding
    var queryEmbedding = await _openAIService.GenerateEmbeddingsAsync(query);
    
    // 2. Configure hybrid search
    var options = new SearchOptions
    {
        SearchMode = Azure.Search.Documents.Models.SearchMode.All,
        VectorSearch = new()
        {
            Queries = { new VectorizedQuery(queryEmbedding.ToArray()) 
            { 
                KNearestNeighborsCount = 15,
                Fields = { "ContentVector" } 
            } }
        },
        Size = 5,
        Select = { "Id", "Title", "Content" }
    };
    
    // 3. Execute search (with text query for BM25)
    var response = await _searchClient.SearchAsync<VectorSearchDocument>(
        query,  // Text query for BM25
        options
    );
    
    // 4. Process results
    var results = new List<SearchResult>();
    await foreach (var result in response.Value.GetResultsAsync())
    {
        results.Add(new SearchResult
        {
            Document = result.Document,
            Score = result.Score ?? 0,
            Highlights = result.Highlights
        });
    }
    
    return results;
}
```

### Example 3: Filtered Vector Search

```csharp
public async Task<List<SearchResult>> SearchWithFilterAsync(
    string query, 
    string category)
{
    var queryEmbedding = await _openAIService.GenerateEmbeddingsAsync(query);
    
    var options = new SearchOptions
    {
        Filter = $"Category eq '{category}'",  // OData filter
        VectorSearch = new()
        {
            Queries = { new VectorizedQuery(queryEmbedding.ToArray()) 
            { 
                KNearestNeighborsCount = 10,
                Fields = { "ContentVector" } 
            } }
        },
        Size = 5
    };
    
    var response = await _searchClient.SearchAsync<VectorSearchDocument>(
        null,
        options
    );
    
    // Process results...
}
```

---

## Interview Q&A

### Q1: What is vector search and how does it work?

**Answer:**
"Vector search converts text into numerical vectors (embeddings) using machine learning models. Instead of matching keywords, it finds documents with similar embeddings using cosine similarity. For example, 'car' and 'automobile' have different keywords but similar embeddings, so vector search finds both."

**Technical Details:**
- Embeddings are high-dimensional vectors (1536 for ada-002)
- Cosine similarity measures angle between vectors
- HNSW algorithm finds approximate nearest neighbors efficiently

---

### Q2: What's the difference between vector search and traditional search?

**Answer:**
"Traditional search (BM25) uses keyword matching and term frequency. It's fast and cheap but misses synonyms. Vector search uses semantic similarity—it understands meaning, not just words. In production, we use hybrid search combining both: BM25 catches exact terms while vectors catch semantic matches."

| Aspect | BM25 (Traditional) | Vector Search |
|--------|-------------------|---------------|
| Matching | Keywords/terms | Semantic meaning |
| Synonyms | ❌ Misses | ✅ Finds |
| Speed | Fast | Slower (embedding) |
| Cost | Cheap | Expensive |
| Multilingual | ❌ No | ✅ Yes |

---

### Q3: What is HNSW and why is it used?

**Answer:**
"HNSW (Hierarchical Navigable Small World) is an approximate nearest neighbor algorithm for fast vector search. It builds a multi-layer graph structure where higher layers are sparse (for fast navigation) and lower layers are dense (for accuracy). This gives logarithmic search complexity instead of linear, making it practical for millions of vectors."

**Parameters:**
- **M:** Bi-directional links per node (4-10)
- **efConstruction:** Build quality (default 400)
- **efSearch:** Search quality (default 500)

---

### Q4: What is hybrid search and why is it better?

**Answer:**
"Hybrid search combines BM25 keyword matching with vector semantic search. It uses Reciprocal Rank Fusion (RRF) to merge both result sets intelligently. This is better because BM25 catches exact terms and proper nouns while vectors handle synonyms and paraphrases. Production systems almost always use hybrid, not pure vector."

**Example:**
- Query: "ML model training"
- BM25 finds: Exact phrase "ML model training"
- Vector finds: "machine learning algorithm development" (semantic match)
- Hybrid returns: Both! (ranked by RRF)

---

### Q5: How do you prevent vector search from being too slow?

**Answer:**
"Several optimizations:
1. **Batch embedding generation** - generate embeddings in parallel
2. **Tune HNSW parameters** - lower efSearch for speed (trade recall)
3. **Use filters** - reduce search space with OData filters
4. **Cache embeddings** - don't regenerate for common queries
5. **Async operations** - don't block on embedding generation
6. **Choose smaller models** - text-embedding-3-small is 5x cheaper than ada-002"

---

### Q6: What's the difference between cosine similarity and dot product?

**Answer:**
"Cosine similarity measures the angle between vectors (0-1 range for normalized vectors). Dot product measures both angle AND magnitude. For text embeddings like OpenAI's (which are normalized), cosine similarity is preferred because it only cares about direction, not magnitude. Formula: `cosine = (A·B) / (||A|| × ||B||)`"

---

### Q7: How do you choose embedding dimensions?

**Answer:**
"The dimensions are determined by your embedding model:
- text-embedding-ada-002: 1536 dimensions
- text-embedding-3-small: 1536 dimensions  
- text-embedding-3-large: 3072 dimensions

You MUST match the index dimensions to your model. Higher dimensions capture more nuance but increase storage and search time. For most use cases, 1536 dimensions (ada-002 or 3-small) is sufficient."

---

### Q8: When would you use vector search vs fine-tuning?

**Answer:**
"Vector search (RAG) vs fine-tuning:

**Use Vector Search (RAG) when:**
- Data changes frequently (just re-index)
- Need transparency (see source documents)
- Multiple knowledge domains
- Cost-sensitive (fine-tuning is expensive)
- Quick implementation

**Use Fine-tuning when:**
- Changing model behavior/style
- Domain-specific language patterns
- Static knowledge
- Performance critical (no retrieval overhead)

In practice, many systems use BOTH: RAG for knowledge, fine-tuning for behavior."

---

### Q9: How do you handle chunking for large documents?

**Answer:**
"Large documents need to be split into chunks because:
1. Embedding models have token limits (8191 for ada-002)
2. Smaller chunks give more precise retrieval
3. Better relevance scores

**Strategies:**
- **Fixed size:** 512-1024 tokens with 50-100 token overlap
- **Semantic:** Split on paragraphs/sections
- **Recursive:** Split by sentence, then paragraph, then section

Each chunk gets its own embedding and document ID."

---

### Q10: What's Reciprocal Rank Fusion (RRF)?

**Answer:**
"RRF is the algorithm that combines multiple result sets in hybrid search. Instead of averaging scores (which can be on different scales), it uses rank position:

```
RRF_score = Σ [ 1 / (k + rank_i) ]
```

Where k=60 (constant). A document ranked #1 in both sets gets higher score than one ranked #1 in one and #100 in the other. It's scale-invariant and works well in practice."

---

## Performance Tuning

### 1. Index Configuration

```csharp
// Fast but less accurate
M = 4
efConstruction = 200
efSearch = 200

// Balanced (recommended)
M = 4
efConstruction = 400
efSearch = 500

// Accurate but slower
M = 8
efConstruction = 800
efSearch = 1000
```

### 2. Query Optimization

```csharp
// Get more candidates for better hybrid results
KNearestNeighborsCount = Top * 3

// Use filters to reduce search space
Filter = "Category eq 'Technology' and CreatedDate gt 2024-01-01"

// Select only needed fields
Select = { "Id", "Title", "Content" }  // Skip vector field

// Use pagination
Size = 10
Skip = 20
```

### 3. Embedding Optimization

```csharp
// Batch embedding generation
var tasks = documents.Select(d => 
    openAIService.GenerateEmbeddingsAsync(d.Content)
);
var embeddings = await Task.WhenAll(tasks);

// Cache common queries
private readonly MemoryCache _embeddingCache = new();

public async Task<float[]> GetEmbeddingAsync(string text)
{
    var cacheKey = $"emb_{text.GetHashCode()}";
    if (_embeddingCache.TryGetValue(cacheKey, out float[] cached))
        return cached;
    
    var embedding = await GenerateEmbeddingAsync(text);
    _embeddingCache.Set(cacheKey, embedding, TimeSpan.FromHours(1));
    return embedding;
}
```

### 4. Cost Optimization

| Optimization | Savings | Tradeoff |
|-------------|---------|----------|
| Use text-embedding-3-small | 5x cheaper | Slightly less accurate |
| Cache embeddings | 90%+ | Memory usage |
| Batch API calls | Less overhead | Slight latency |
| Filter before search | Less compute | Need filterable fields |

---

## Summary

### Key Takeaways for Interviews:

1. **Vector Search** = Semantic similarity using embeddings
2. **HNSW** = Fast approximate nearest neighbor algorithm
3. **Hybrid Search** = BM25 + Vector + RRF (best in production)
4. **Dimensions** = Must match embedding model (1536 for ada-002)
5. **Cosine Similarity** = Measures angle between vectors (0-1)
6. **Use Cases** = Semantic search, recommendations, RAG systems

### Quick Reference:

```bash
# 1. Create Index
POST /api/VectorSearch/create-index

# 2. Add Documents
POST /api/VectorSearch/seed-data

# 3. Search
POST /api/VectorSearch/hybrid-search
{
  "query": "What is machine learning?",
  "top": 5,
  "filter": "category eq 'AI'"
}

# 4. Compare Modes
GET /api/VectorSearch/compare?query=semantic+search&top=3
```

### Files Created:
- [VectorSearchModels.cs](AzureOpenAISample/Models/VectorSearchModels.cs) - Models with vector fields
- [VectorSearchService.cs](AzureOpenAISample/Services/VectorSearchService.cs) - Core implementation
- [VectorSearchController.cs](AzureOpenAISample/Controllers/VectorSearchController.cs) - REST API
- [Program.cs](AzureOpenAISample/Program.cs) - Service registration
- [appsettings.json](AzureOpenAISample/appsettings.json) - Configuration

---

**Ready for your interview!** 🚀
