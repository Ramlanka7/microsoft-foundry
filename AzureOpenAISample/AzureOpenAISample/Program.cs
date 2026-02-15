using AzureOpenAISample.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Azure Services Demo API",
        Version = "v1",
        Description = "Demo API showcasing Azure OpenAI, Cognitive Search, Blob Storage, App Insights, and Managed Identity"
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ===== APPLICATION INSIGHTS =====
// Interview Talking Points:
// - Application Insights provides telemetry, logging, and monitoring
// - Automatic request tracking, dependency tracking, performance counters
// - Custom events, metrics, and traces
// - Live metrics for real-time monitoring
// - Distributed tracing for microservices
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// ===== REGISTER AZURE SERVICES =====
// Interview Tip: Explain Dependency Injection and service lifetimes (Singleton, Scoped, Transient)
// Singleton: One instance for the application lifetime (thread-safe services)
// Scoped: One instance per request (database contexts)
// Transient: New instance each time (lightweight, stateless services)

builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddSingleton<IAzureCognitiveSearchService, AzureCognitiveSearchService>();
builder.Services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();

// ===== FOUNDRY SERVICE =====
// Service to interact with models deployed via Azure AI Foundry (Model Catalog)
// Uses Azure.AI.Inference SDK
builder.Services.AddSingleton<IFoundryService, FoundryService>();

// ===== VECTOR SEARCH SERVICE =====
// Interview Tip: Vector Search uses embeddings for semantic similarity
// Combines Azure Cognitive Search + Azure OpenAI embeddings
// Supports pure vector search and hybrid search (BM25 + vectors)
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();

// ===== RAG SERVICE =====
// Interview Tip: RAG (Retrieval Augmented Generation) combines search + LLM generation
// This prevents hallucination and enables domain-specific knowledge without fine-tuning
builder.Services.AddSingleton<IRagService, RagService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure Services Demo API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}).WithName("HealthCheck");

// Add an info endpoint showing configuration
app.MapGet("/api/info", (IConfiguration config) => new
{
    dotnetVersion = Environment.Version.ToString(),
    targetFramework = "net8.0",
    azureServices = new
    {
        openAI = new
        {
            configured = !string.IsNullOrEmpty(config["AzureOpenAI:Endpoint"]),
            useManagedIdentity = config.GetValue<bool>("AzureOpenAI:UseManagedIdentity")
        },
        cognitiveSearch = new
        {
            configured = !string.IsNullOrEmpty(config["AzureCognitiveSearch:Endpoint"]),
            useManagedIdentity = config.GetValue<bool>("AzureCognitiveSearch:UseManagedIdentity")
        },
        blobStorage = new
        {
            configured = !string.IsNullOrEmpty(config["AzureBlobStorage:ContainerName"]),
            useManagedIdentity = config.GetValue<bool>("AzureBlobStorage:UseManagedIdentity")
        },
        applicationInsights = new
        {
            configured = !string.IsNullOrEmpty(config["ApplicationInsights:ConnectionString"])
        }
    }
}).WithName("SystemInfo");

app.Run();
