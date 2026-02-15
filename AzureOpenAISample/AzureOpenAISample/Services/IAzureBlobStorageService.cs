using AzureOpenAISample.Models;

namespace AzureOpenAISample.Services;

/// <summary>
/// Service for Azure Blob Storage operations
/// 
/// Interview Topics:
/// - Blob types: Block blobs (files), Page blobs (VHDs), Append blobs (logs)
/// - Access tiers: Hot, Cool, Archive (cost vs performance)
/// - SAS tokens for secure temporary access
/// - Metadata and properties
/// - Lifecycle management and versioning
/// </summary>
public interface IAzureBlobStorageService
{
    /// <summary>
    /// Upload a blob from stream
    /// </summary>
    Task<AzureOpenAISample.Models.BlobInfo> UploadBlobAsync(string blobName, Stream content, string contentType, Dictionary<string, string>? metadata = null);
    
    /// <summary>
    /// Download a blob to stream
    /// </summary>
    Task<Stream> DownloadBlobAsync(string blobName);
    
    /// <summary>
    /// List all blobs in the container
    /// </summary>
    Task<List<AzureOpenAISample.Models.BlobInfo>> ListBlobsAsync(string? prefix = null);
    
    /// <summary>
    /// Delete a blob
    /// </summary>
    Task<bool> DeleteBlobAsync(string blobName);
    
    /// <summary>
    /// Get blob metadata without downloading content
    /// </summary>
    Task<AzureOpenAISample.Models.BlobInfo?> GetBlobPropertiesAsync(string blobName);
    
    /// <summary>
    /// Generate a SAS URL for temporary access
    /// Interview Tip: SAS tokens provide time-limited access without sharing keys
    /// </summary>
    Task<string> GenerateSasUrlAsync(string blobName, TimeSpan validity);
    
    /// <summary>
    /// Copy a blob within the same storage account
    /// </summary>
    Task<bool> CopyBlobAsync(string sourceBlobName, string destinationBlobName);
}
