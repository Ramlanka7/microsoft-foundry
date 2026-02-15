# Azure Services Learning Project - .NET 8 Web API

> **🚀 NEW TO THIS PROJECT?** Start with **[START_HERE.md](START_HERE.md)** for a complete guide!

## 📚 Overview
This project provides comprehensive, interview-ready examples of key Azure services integrated with .NET 8 Web API. Each service includes detailed code comments, best practices, and interview talking points.

## 🎯 Technologies Covered

### 1. **.NET 8 Web API**
- Modern C# features
- Minimal API endpoints
- Dependency Injection
- Swagger/OpenAPI documentation

### 2. **Azure OpenAI SDK**
- Chat completions
- Streaming responses
- Embeddings generation
- Token management
- Both API Key and Managed Identity authentication

### 3. **Azure Cognitive Search**
- Full-text search with BM25 ranking
- Document indexing (single and batch)
- Filtering with OData syntax
- Faceted navigation
- Semantic search capabilities

### 4. **Azure Blob Storage**
- File upload/download
- List and manage blobs
- Metadata management
- SAS token generation
- Blob copy operations
- Multiple authentication methods

### 5. **Application Insights**
- Automatic request tracking
- Custom events and metrics
- Exception tracking
- Dependency tracking
- Performance monitoring
- Live metrics

### 6. **Managed Identity**
- System-assigned and User-assigned identities
- Eliminates credential management in code
- Integrated with all Azure services
- Azure RBAC for fine-grained permissions

### 7. **RAG (Retrieval Augmented Generation)** ⭐ NEW!
- Complete RAG pipeline implementation
- Combines Azure Cognitive Search with Azure OpenAI
- Prevents hallucination with grounded answers
- Source citations for transparency
- Document ingestion with embeddings
- Production-ready architecture

### 8. **Vector Search (Semantic Search)** ⭐ NEW!
- Azure Cognitive Search with vector capabilities
- HNSW algorithm for fast nearest neighbor search
- Pure vector search (semantic similarity)
- Hybrid search (BM25 + vectors with RRF)
- Auto-embedding generation with OpenAI
- Cosine similarity matching
- 1536-dimensional embeddings (text-embedding-ada-002)

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Azure subscription
- Visual Studio 2022 or VS Code

### Azure Resources Required
1. **Azure OpenAI**: Create an Azure OpenAI resource and deploy a model
2. **Azure Cognitive Search**: Create a search service and index
3. **Azure Storage Account**: Create a storage account and container
4. **Application Insights**: Create an Application Insights resource

### Configuration

Update `appsettings.json` with your Azure resource details:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-35-turbo",
    "UseManagedIdentity": false
  },
  "AzureCognitiveSearch": {
    "Endpoint": "https://your-search.search.windows.net",
    "ApiKey": "your-api-key",
    "IndexName": "sample-index",
    "UseManagedIdentity": false
  },
  "AzureBlobStorage": {
    "ConnectionString": "your-connection-string",
    "ContainerName": "sample-container",
    "UseManagedIdentity": false,
    "AccountName": "yourstorageaccount"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;..."
  }
}
```

### Run the Application

```bash
cd AzureOpenAISample
dotnet restore
dotnet build
dotnet run
```

The API will be available at `https://localhost:7xxx` with Swagger UI at the root.

---

## 📖 API Endpoints

### Azure OpenAI Controller (`/api/AzureOpenAI`)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/test` | GET | Test Azure OpenAI connectivity |
| `/chat` | POST | Get chat completion |
| `/chat-stream` | POST | Get streaming chat completion |
| `/embeddings` | POST | Generate text embeddings |

**Example Chat Request:**
```json
{
  "message": "Explain dependency injection in .NET",
  "maxTokens": 500,
  "temperature": 0.7
}
```

### Cognitive Search Controller (`/api/CognitiveSearch`)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/search` | POST | Search documents |
| `/index` | POST | Index a single document |
| `/index-batch` | POST | Index multiple documents |
| `/seed-sample-data` | POST | Create sample documents |
| `/document/{id}` | GET | Get document by ID |
| `/document/{id}` | DELETE | Delete document |

**Example Search Request:**
```json
{
  "searchText": ".NET Core",
  "top": 10,
  "filter": "category eq 'Technology'",
  "facets": ["category", "tags"]
}
```

### Blob Storage Controller (`/api/BlobStorage`)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/upload` | POST | Upload file (multipart) |
| `/upload-text` | POST | Upload text content |
| `/download/{blobName}` | GET | Download blob |
| `/list` | GET | List all blobs |
| `/properties/{blobName}` | GET | Get blob properties |
| `/sas/{blobName}` | POST | Generate SAS URL |
| `/copy` | POST | Copy blob |
| `/{blobName}` | DELETE | Delete blob |

### Telemetry Controller (`/api/Telemetry`)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/demo` | GET | Demonstrate all telemetry types |
| `/custom-event` | POST | Track custom event |
| `/custom-metric` | POST | Track custom metric |
| `/performance-test` | GET | Performance monitoring demo |
| `/error-test` | GET | Exception tracking demo |

### RAG Controller (`/api/Rag`) ⭐ NEW!

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/query` | POST | Query RAG system (retrieve + generate) |
| `/ingest` | POST | Add document to knowledge base |
| `/ingest-batch` | POST | Batch add documents |
| `/seed-knowledge-base` | POST | Create sample documents |
| `/search-similar` | POST | Test document retrieval |
| `/demo` | GET | RAG API documentation |

**Example RAG Query:**
```json
{
  "query": "What is RAG and how does it work?",
  "maxSearchResults": 3,
  "maxTokens": 500,
  "temperature": 0.5,
  "includeSourceReferences": true
}
```

**RAG Response:**
```json
{
  "answer": "RAG (Retrieval Augmented Generation) combines...",
  "sources": [
    {
      "documentId": "rag-doc-2",
      "title": "What is RAG?",
      "relevanceScore": 1.0
    }
  ],
  "tokensUsed": 342,
  "documentsRetrieved": 3
}
```

### Utility Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/api/info` | GET | System and configuration info |

---

## 🤖 RAG (Retrieval Augmented Generation)

**What is RAG?**
RAG combines document retrieval (search) with LLM generation to provide factually grounded answers. Instead of relying solely on the model's training data, RAG retrieves relevant documents and uses them as context.

**Benefits:**
- ✅ Prevents hallucination
- ✅ Always up-to-date (just update documents)
- ✅ Domain expertise without fine-tuning
- ✅ Transparent source citations
- ✅ Cost-effective

**Quick Start:**
```bash
# 1. Seed knowledge base
POST /api/Rag/seed-knowledge-base

# 2. Ask questions
POST /api/Rag/query
{
  "query": "What is RAG?",
  "maxSearchResults": 3
}
```

**Documentation:**
- [RAG_GUIDE.md](RAG_GUIDE.md) - Comprehensive 30-page guide
- [RAG_SUMMARY.md](RAG_SUMMARY.md) - Quick reference
- [RAG_VISUAL.txt](RAG_VISUAL.txt) - Visual diagrams

---

## 🎓 Interview Preparation Guide

### RAG (Retrieval Augmented Generation) ⭐
**Key Concepts:**
- What is RAG and why use it?
- Retrieval → Augmentation → Generation pipeline
- Prevents hallucination with grounded context
- RAG vs fine-tuning tradeoffs
- Chunking strategies for documents
- Hybrid search (keyword + semantic)

**Sample Questions:**
1. *"What is RAG?"*
   - Combines retrieval with generation for factual answers
2. *"Why use RAG over fine-tuning?"*
   - Cost-effective, always current, transparent sources

### Azure OpenAI
**Key Concepts:**
- Difference between Azure OpenAI and OpenAI API
- Token limits and pricing model
- Temperature vs Top-P sampling
- System vs User messages
- Embeddings for semantic search
- Streaming for better UX

**Sample Questions:**
1. *"How would you implement rate limiting for Azure OpenAI?"*
   - Use token bucket algorithm, track usage per user, implement retry with exponential backoff
2. *"What's the difference between embeddings and completions?"*
   - Embeddings convert text to vectors for similarity; completions generate text

### Azure Cognitive Search
**Key Concepts:**
- BM25 ranking algorithm
- Semantic vs keyword search
- Vector search with embeddings
- Faceting and filtering (OData)
- Indexing strategies
- Search relevance tuning

**Sample Questions:**
1. *"How do you optimize search performance?"*
   - Partition keys, replica scaling, select only needed fields, use filters before queries
2. *"Explain semantic search"*
   - Uses AI to understand intent, not just keywords; considers context and meaning

### Azure Blob Storage
**Key Concepts:**
- Blob types: Block, Page, Append
- Access tiers: Hot, Cool, Archive
- SAS tokens vs Managed Identity
- Lifecycle management
- CDN integration
- Soft delete and versioning

**Sample Questions:**
1. *"When would you use Page blobs vs Block blobs?"*
   - Page blobs for random read/write (VHDs); Block blobs for files and streams
2. *"How do you secure blob access?"*
   - Managed Identity > SAS tokens > Access keys; use RBAC, private endpoints

### Application Insights
**Key Concepts:**
- Telemetry types: Request, Dependency, Event, Metric, Trace, Exception
- Distributed tracing with correlation ID
- Custom telemetry vs automatic
- KQL (Kusto Query Language)
- Sampling for cost control
- Live metrics

**Sample Questions:**
1. *"How do you track custom business metrics?"*
   - Use TrackEvent for events, TrackMetric for measurements; add custom properties
2. *"Explain distributed tracing"*
   - Correlation ID links telemetry across services; see full request flow in transaction search

### Managed Identity
**Key Concepts:**
- System-assigned vs User-assigned
- DefaultAzureCredential chain
- Azure RBAC roles
- No secrets in code or configuration
- Works in Azure (App Service, AKS, VMs)
- Local development with Azure CLI/VS

**Sample Questions:**
1. *"What's the benefit of Managed Identity?"*
   - No credentials in code, automatic rotation, Azure manages lifecycle, audit trail
2. *"System-assigned vs User-assigned?"*
   - System: tied to resource, deleted with resource; User: independent, reusable across resources

---

## 🔐 Managed Identity Setup

### For Local Development:
```bash
# Login with Azure CLI
az login

# Your Azure credentials will be used automatically
# DefaultAzureCredential will use Azure CLI credentials locally
```

### For Azure App Service:
1. Enable System-assigned Managed Identity in Azure Portal
2. Assign roles to the identity:
   ```bash
   # Assign Cognitive Services OpenAI User role
   az role assignment create \
     --assignee <identity-object-id> \
     --role "Cognitive Services OpenAI User" \
     --scope <resource-id>
   ```
3. Set `UseManagedIdentity: true` in configuration

### Required Azure RBAC Roles:
- **Azure OpenAI**: `Cognitive Services OpenAI User`
- **Cognitive Search**: `Search Index Data Contributor`
- **Blob Storage**: `Storage Blob Data Contributor`

---

## 📊 Dependency Injection Pattern

The application uses constructor injection for all services:

```csharp
// In Program.cs
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddSingleton<IAzureCognitiveSearchService, AzureCognitiveSearchService>();
builder.Services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();

// In Controllers
public class MyController : ControllerBase
{
    private readonly IAzureOpenAIService _openAIService;
    
    public MyController(IAzureOpenAIService openAIService)
    {
        _openAIService = openAIService;
    }
}
```

**Service Lifetimes:**
- **Singleton**: Services are stateless and thread-safe (Azure SDK clients)
- **Scoped**: Services needed per request (DbContext)
- **Transient**: Lightweight, stateless services

---

## 🧪 Testing the API

### Using Swagger UI
1. Run the application
2. Navigate to `https://localhost:7xxx`
3. Try each endpoint interactively

### Using cURL

**Test Azure OpenAI:**
```bash
curl -X POST https://localhost:7xxx/api/AzureOpenAI/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Explain microservices",
    "maxTokens": 500,
    "temperature": 0.7
  }'
```

**Seed Search Data:**
```bash
curl -X POST https://localhost:7xxx/api/CognitiveSearch/seed-sample-data
```

**Search Documents:**
```bash
curl -X POST https://localhost:7xxx/api/CognitiveSearch/search \
  -H "Content-Type: application/json" \
  -d '{
    "searchText": "Azure",
    "top": 5
  }'
```

**Upload Text to Blob:**
```bash
curl -X POST https://localhost:7xxx/api/BlobStorage/upload-text \
  -H "Content-Type: application/json" \
  -d '{
    "blobName": "test.txt",
    "content": "Hello Azure Blob Storage!",
    "contentType": "text/plain"
  }'
```

---

## 🏗️ Project Structure

```
AzureOpenAISample/
├── Controllers/
│   ├── AzureOpenAIController.cs      # Azure OpenAI endpoints
│   ├── CognitiveSearchController.cs   # Search endpoints
│   ├── BlobStorageController.cs       # Blob storage endpoints
│   ├── TelemetryController.cs         # App Insights demo
│   └── WeatherForecastController.cs   # Default template
├── Services/
│   ├── IAzureOpenAIService.cs         # OpenAI interface
│   ├── AzureOpenAIService.cs          # OpenAI implementation
│   ├── IAzureCognitiveSearchService.cs
│   ├── AzureCognitiveSearchService.cs
│   ├── IAzureBlobStorageService.cs
│   └── AzureBlobStorageService.cs
├── Models/
│   ├── ChatRequest.cs                 # OpenAI models
│   ├── SearchModels.cs                # Search models
│   └── BlobModels.cs                  # Blob models
├── Program.cs                         # Application startup
└── appsettings.json                   # Configuration
```

---

## 💡 Best Practices Demonstrated

1. **Async/Await**: All I/O operations are async
2. **Dependency Injection**: Loose coupling, testable code
3. **Configuration Management**: Settings in appsettings.json
4. **Error Handling**: Try-catch with logging and proper HTTP status codes
5. **Logging**: ILogger integration with Application Insights
6. **API Documentation**: Swagger/OpenAPI with XML comments
7. **Security**: Managed Identity support, no hardcoded secrets
8. **Resource Management**: Proper disposal of streams and clients

---

## 🔍 Common Interview Questions

### General .NET
1. **Dependency Injection**: Explain the three lifetimes (Singleton, Scoped, Transient)
2. **Async/Await**: Why use async? What's the difference between Task and ValueTask?
3. **Middleware Pipeline**: Explain ASP.NET Core request pipeline
4. **Configuration**: How do you manage different environments?

### Azure Services
1. **Cost Optimization**: How do you minimize Azure costs?
2. **Security**: How do you secure Azure resources?
3. **Scalability**: How do you design for scale?
4. **Monitoring**: How do you monitor application health?

### Architecture
1. **Microservices**: When to use microservices vs monolith?
2. **Event-Driven**: When to use message queues?
3. **CQRS**: Explain Command Query Responsibility Segregation
4. **Distributed Systems**: How do you handle failures?

---

## 📚 Additional Resources

### Official Documentation
- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure Cognitive Search Documentation](https://learn.microsoft.com/azure/search/)
- [Azure Blob Storage Documentation](https://learn.microsoft.com/azure/storage/blobs/)
- [Application Insights Documentation](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Managed Identity Documentation](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/)

### Learning Paths
- [Microsoft Learn - Azure Developer](https://learn.microsoft.com/training/paths/azure-developer/)
- [.NET 8 What's New](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8)

---

## 🤝 Contributing

This is a learning project. Feel free to:
- Add more examples
- Improve documentation
- Fix issues
- Share interview experiences

---

## 📝 License

MIT License - Use this code for learning and interviews!

---

## ✨ Quick Start Checklist

- [ ] Install .NET 8 SDK
- [ ] Create Azure resources
- [ ] Update appsettings.json with your Azure details
- [ ] Run `dotnet restore`
- [ ] Run `dotnet run`
- [ ] Open browser to Swagger UI
- [ ] Test each endpoint
- [ ] Review code comments
- [ ] Practice explaining concepts

**Good luck with your interviews! 🚀**
