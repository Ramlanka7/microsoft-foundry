# Quick Interview Reference Guide

## 🔥 One-Liner Explanations

### .NET 8 Web API
*"Latest LTS version of .NET with improved performance, minimal APIs, and native AOT support."*

### Azure OpenAI SDK
*"Managed service providing OpenAI models like GPT-4 through Azure with enterprise security and compliance."*

### Azure Cognitive Search
*"AI-powered search service with full-text search, semantic search, and vector search capabilities."*

### Azure Blob Storage
*"Object storage for unstructured data with three access tiers (Hot/Cool/Archive) for cost optimization."*

### Application Insights
*"APM tool providing real-time monitoring, telemetry, distributed tracing, and log analytics using KQL."*

### Managed Identity
*"Azure AD identity that eliminates credentials in code - Azure manages authentication automatically."*

---

## ⚡ 30-Second Explanations

### 1. Azure OpenAI
**What:** Access to OpenAI models (GPT-4, GPT-3.5) through Azure infrastructure.

**Key Points:**
- Uses Azure security, compliance, and private networking
- Token-based pricing (input + output tokens)
- Supports chat completions, embeddings, and streaming
- Rate limits per region/deployment

**Code Pattern:**
```csharp
var client = new OpenAIClient(endpoint, new DefaultAzureCredential());
var response = await client.GetChatCompletionsAsync(options);
```

**Common Interview Q:** *"Azure OpenAI vs OpenAI API?"*
- Azure: Enterprise security, compliance, Managed Identity, private endpoints
- OpenAI: Faster model updates, broader availability

---

### 2. Azure Cognitive Search
**What:** Search-as-a-service with AI capabilities.

**Key Points:**
- BM25 ranking algorithm (bag-of-words statistical ranking)
- Supports indexing, querying, faceting, filtering (OData)
- Vector search for semantic similarity
- Scales with replicas (queries) and partitions (indexing)

**Code Pattern:**
```csharp
var client = new SearchClient(endpoint, indexName, credential);
var results = await client.SearchAsync<T>(searchText, options);
```

**Common Interview Q:** *"How to optimize search performance?"*
- Select only needed fields
- Use filters before search (reduces result set)
- Appropriate replica/partition sizing
- Caching for frequent queries

---

### 3. Azure Blob Storage
**What:** Object storage for files, images, videos, backups.

**Key Points:**
- Three blob types: Block (files), Page (VHDs), Append (logs)
- Three tiers: Hot (frequent access), Cool (30+ days), Archive (180+ days)
- Security: Private endpoints, SAS tokens, Managed Identity
- Features: Versioning, soft delete, lifecycle policies

**Code Pattern:**
```csharp
var client = new BlobServiceClient(uri, credential);
var container = client.GetBlobContainerClient(containerName);
await container.UploadBlobAsync(blobName, stream);
```

**Common Interview Q:** *"When to use each access tier?"*
- Hot: Active data, frequently accessed ($$ storage, $ operations)
- Cool: Backups, recent archives ($ storage, $$ operations)
- Archive: Long-term retention, compliance ($$$$ retrieval, hours to rehydrate)

---

### 4. Application Insights
**What:** Application Performance Management (APM) and monitoring.

**Key Points:**
- Automatic tracking: requests, dependencies, exceptions
- Custom telemetry: events, metrics, traces
- Distributed tracing with correlation IDs
- Live metrics, smart detection, alerts

**Code Pattern:**
```csharp
_telemetryClient.TrackEvent("OrderPlaced", 
    properties: new Dictionary<string, string> { ["UserId"] = userId },
    metrics: new Dictionary<string, double> { ["Amount"] = 99.99 });
```

**Common Interview Q:** *"How to track a complex operation across services?"*
- Use correlation ID (automatic in App Insights)
- TrackDependency for external calls
- Parent-child operation relationship
- Query with KQL in Application Insights

---

### 5. Managed Identity
**What:** Azure AD identity for Azure resources to authenticate without credentials.

**Key Points:**
- System-Assigned: 1:1 with resource, deleted with resource
- User-Assigned: Standalone, reusable across resources
- DefaultAzureCredential: Works locally (CLI) and in Azure (MI)
- Requires Azure RBAC role assignment

**Code Pattern:**
```csharp
// Works locally AND in Azure with no code changes!
var credential = new DefaultAzureCredential();
var client = new BlobServiceClient(uri, credential);
```

**Common Interview Q:** *"How does it work?"*
1. Resource requests token from Azure Instance Metadata Service
2. IMDS validates with Azure AD
3. Azure AD issues short-lived token (1-24 hours)
4. Token auto-refreshes before expiration

---

## 🎯 Common Architecture Questions

### Q: How would you design a document search system?
**A:** 
1. Store documents in Blob Storage (raw files)
2. Process with Azure Functions (triggered by blob upload)
3. Generate embeddings with Azure OpenAI
4. Index in Cognitive Search with vectors
5. Search combines keyword + semantic (vector) search
6. Monitor with Application Insights

### Q: How do you secure Azure resources?
**A:**
1. Use Managed Identity (no secrets in code)
2. Private endpoints (no public access)
3. Azure RBAC (least privilege)
4. Key Vault for application secrets
5. Network security groups and firewalls
6. Monitoring and alerts with App Insights

### Q: How do you optimize costs in Azure?
**A:**
1. Right-size resources (don't over-provision)
2. Use appropriate storage tiers (Hot/Cool/Archive)
3. Implement caching (reduce API calls)
4. Use reserved instances for predictable workloads
5. Set up budgets and cost alerts
6. Monitor with Azure Cost Management

---

## 📊 Comparison Tables

### Authentication Methods

| Method | Security | Ease of Use | Use Case |
|--------|----------|-------------|----------|
| Managed Identity | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | Azure resources (Production) |
| Service Principal | ⭐⭐⭐⭐ | ⭐⭐⭐ | CI/CD, non-Azure |
| API Key | ⭐⭐ | ⭐⭐⭐⭐⭐ | Development only |
| Connection String | ⭐ | ⭐⭐⭐⭐⭐ | Legacy, avoid |

### Azure Storage Options

| Service | Use Case | Access Pattern |
|---------|----------|----------------|
| Blob Storage | Files, images, backups | Object storage |
| File Storage | Shared file systems | SMB protocol |
| Queue Storage | Messaging | FIFO queue |
| Table Storage | NoSQL key-value | Structured data |
| Disk Storage | VM disks | Block storage |

### Search Approaches

| Type | Best For | Latency | Cost |
|------|----------|---------|------|
| Full-text (BM25) | Exact keywords | Low | $ |
| Semantic | Intent understanding | Medium | $$ |
| Vector | Similarity | Medium | $$ |
| Hybrid | Best of all | Medium | $$$ |

---

## 🏆 Interview Winning Phrases

### Show Azure Expertise
- *"I use DefaultAzureCredential for seamless local-to-production deployment"*
- *"We implemented vector search with Azure OpenAI embeddings for semantic understanding"*
- *"I monitor token usage to optimize OpenAI costs"*
- *"We use Application Insights correlation IDs for distributed tracing"*
- *"I always follow the principle of least privilege with Azure RBAC"*

### Show .NET Expertise
- *"I use Singleton lifetime for Azure SDK clients as they're thread-safe"*
- *"All I/O operations are async to prevent thread pool starvation"*
- *"I leverage IOptions pattern for strongly-typed configuration"*
- *"We use structured logging for better query performance in App Insights"*

### Show Architecture Thinking
- *"I design for failure - retry with exponential backoff for transient errors"*
- *"We implement circuit breaker pattern for external dependencies"*
- *"I use the Outbox pattern for reliable event publishing"*
- *"We follow CQRS to separate read and write concerns"*

---

## 💡 Common Mistakes to Avoid

### In Interviews
❌ *"I just use connection strings"* → ✅ *"I use Managed Identity for security"*
❌ *"Azure OpenAI is just OpenAI in Azure"* → ✅ *"It provides enterprise security and compliance"*
❌ *"I don't know about monitoring"* → ✅ *"I use Application Insights for telemetry"*
❌ *"Search is just SQL LIKE"* → ✅ *"Cognitive Search uses BM25 ranking with semantic capabilities"*

### In Code
❌ Hardcoded credentials
❌ Synchronous I/O operations
❌ No error handling
❌ No logging/telemetry
❌ Not disposing resources
❌ Using Transient lifetime for SDK clients

---

## 📝 Quick Code Snippets

### Error Handling Pattern
```csharp
try
{
    var result = await _service.DoSomethingAsync();
    _logger.LogInformation("Operation succeeded");
    return Ok(result);
}
catch (RequestFailedException ex) when (ex.Status == 429)
{
    _logger.LogWarning("Rate limit exceeded");
    return StatusCode(429, "Too many requests");
}
catch (RequestFailedException ex)
{
    _logger.LogError(ex, "Azure service error");
    return StatusCode(500, new { error = ex.Message });
}
catch (Exception ex)
{
    _telemetryClient.TrackException(ex);
    throw;
}
```

### Retry Pattern
```csharp
var retryOptions = new RetryOptions
{
    MaxRetries = 3,
    Delay = TimeSpan.FromSeconds(2),
    Mode = RetryMode.Exponential
};

var clientOptions = new BlobClientOptions
{
    Retry = retryOptions
};
```

### Dependency Injection
```csharp
// Register
builder.Services.AddSingleton<IMyService, MyService>();

// Use
public class MyController 
{
    private readonly IMyService _service;
    
    public MyController(IMyService service)
    {
        _service = service;
    }
}
```

---

## 🎓 Final Tips

### Before the Interview
1. ✅ Review this guide
2. ✅ Run all APIs in this project
3. ✅ Practice explaining each concept
4. ✅ Prepare 2-3 real project stories
5. ✅ Review Azure pricing pages

### During the Interview
1. ✅ Start with high-level explanation
2. ✅ Use concrete examples
3. ✅ Mention trade-offs
4. ✅ Discuss security and costs
5. ✅ Ask clarifying questions

### Key Success Factors
- **Depth over Breadth**: Know your technologies deeply
- **Real Experience**: Share actual problems you solved
- **Current Knowledge**: Mention latest features (RAG, vector search, etc.)
- **Best Practices**: Security, performance, costs
- **Business Value**: Always connect tech to business outcomes

---

**Remember:** Confidence comes from understanding, not memorization. Use this project to build real understanding!

**Good luck! 🚀**
