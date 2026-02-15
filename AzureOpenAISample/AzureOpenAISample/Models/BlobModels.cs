namespace AzureOpenAISample.Models;

/// <summary>
/// Model for blob metadata
/// </summary>
public class BlobInfo
{
    public string Name { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset? LastModified { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Upload request model
/// </summary>
public class BlobUploadRequest
{
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public Dictionary<string, string>? Metadata { get; set; }
}
