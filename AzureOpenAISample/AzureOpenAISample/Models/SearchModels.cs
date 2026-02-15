namespace AzureOpenAISample.Models;

/// <summary>
/// Model representing a document in Cognitive Search
/// Interview Tip: This represents a searchable document with various field types
/// </summary>
public class SearchDocument
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedDate { get; set; }
    public string[]? Tags { get; set; }
}

/// <summary>
/// Search query request
/// </summary>
public class SearchQuery
{
    public string SearchText { get; set; } = "*"; // * means search all
    public int Top { get; set; } = 10; // Number of results
    public string? Filter { get; set; } // OData filter expression
    public string[]? Facets { get; set; } // Fields to facet on
}

/// <summary>
/// Search results response
/// </summary>
public class SearchResults
{
    public long TotalCount { get; set; }
    public List<SearchDocument> Documents { get; set; } = new();
    public Dictionary<string, List<FacetResult>>? Facets { get; set; }
}

public class FacetResult
{
    public object? Value { get; set; }
    public long Count { get; set; }
}
