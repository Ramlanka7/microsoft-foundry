using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using AzureOpenAISample.Models;

namespace AzureOpenAISample.Services;

/// <summary>
/// Implementation of Azure Blob Storage Service
/// 
/// Interview Key Points:
/// 1. BlobServiceClient - top-level client for storage account
/// 2. BlobContainerClient - client for a specific container
/// 3. BlobClient - client for a specific blob
/// 4. Supports both Connection String and Managed Identity
/// 5. Async operations for better scalability
/// 6. Proper stream handling and disposal
/// </summary>
public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly bool _useManagedIdentity;

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var containerName = configuration["AzureBlobStorage:ContainerName"] 
            ?? throw new ArgumentNullException("AzureBlobStorage:ContainerName");
        
        _useManagedIdentity = configuration.GetValue<bool>("AzureBlobStorage:UseManagedIdentity");

        if (_useManagedIdentity)
        {
            // Use Managed Identity
            var accountName = configuration["AzureBlobStorage:AccountName"] 
                ?? throw new ArgumentNullException("AzureBlobStorage:AccountName");
            var blobServiceUri = new Uri($"https://{accountName}.blob.core.windows.net");
            
            _logger.LogInformation("Using Managed Identity for Blob Storage");
            var blobServiceClient = new BlobServiceClient(blobServiceUri, new DefaultAzureCredential());
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }
        else
        {
            // Use Connection String
            var connectionString = configuration["AzureBlobStorage:ConnectionString"] 
                ?? throw new ArgumentNullException("AzureBlobStorage:ConnectionString");
            
            _logger.LogInformation("Using Connection String for Blob Storage");
            _containerClient = new BlobContainerClient(connectionString, containerName);
        }

        // Create container if it doesn't exist
        _containerClient.CreateIfNotExistsAsync(PublicAccessType.None).Wait();
    }

    /// <summary>
    /// Interview Talking Points:
    /// - UploadAsync supports overwrite by default
    /// - ContentType is important for browsers to handle files correctly
    /// - Metadata is key-value pairs stored with blob (useful for categorization)
    /// - BlobHttpHeaders includes content-type, cache-control, content-encoding
    /// - Progress reporting available through IProgress<long>
    /// </summary>
    public async Task<AzureOpenAISample.Models.BlobInfo> UploadBlobAsync(
        string blobName, 
        Stream content, 
        string contentType, 
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata
            };

            _logger.LogInformation("Uploading blob: {BlobName}, Size: {Size} bytes", blobName, content.Length);
            await blobClient.UploadAsync(content, uploadOptions);

            var properties = await blobClient.GetPropertiesAsync();
            
            return new AzureOpenAISample.Models.BlobInfo
            {
                Name = blobName,
                Uri = blobClient.Uri.ToString(),
                Size = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType,
                LastModified = properties.Value.LastModified,
                Metadata = new Dictionary<string, string>(properties.Value.Metadata)
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to upload blob");
            throw new ApplicationException($"Upload Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Interview Talking Points:
    /// - DownloadAsync returns BlobDownloadInfo with Content stream
    /// - Stream should be disposed properly (use using statement)
    /// - For large files, consider downloading to file directly (DownloadToAsync)
    /// - Can specify byte range for partial downloads
    /// - ETag support for conditional downloads
    /// </summary>
    public async Task<Stream> DownloadBlobAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Blob '{blobName}' not found");
            }

            _logger.LogInformation("Downloading blob: {BlobName}", blobName);
            var response = await blobClient.DownloadAsync();
            
            // Copy to memory stream to avoid connection timeout issues
            var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            return memoryStream;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to download blob");
            throw new ApplicationException($"Download Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Interview Talking Points:
    /// - GetBlobsAsync returns IAsyncEnumerable for efficient pagination
    /// - Prefix filtering is server-side (efficient for large containers)
    /// - Can retrieve blob metadata without downloading content
    /// - Traits parameter controls what information is returned
    /// - States parameter can filter by snapshot, deleted blobs, etc.
    /// </summary>
    public async Task<List<AzureOpenAISample.Models.BlobInfo>> ListBlobsAsync(string? prefix = null)
    {
        try
        {
            var blobs = new List<AzureOpenAISample.Models.BlobInfo>();

            _logger.LogInformation("Listing blobs with prefix: {Prefix}", prefix ?? "none");
            
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobs.Add(new AzureOpenAISample.Models.BlobInfo
                {
                    Name = blobItem.Name,
                    Uri = _containerClient.GetBlobClient(blobItem.Name).Uri.ToString(),
                    Size = blobItem.Properties.ContentLength ?? 0,
                    ContentType = blobItem.Properties.ContentType ?? "unknown",
                    LastModified = blobItem.Properties.LastModified,
                    Metadata = new Dictionary<string, string>(blobItem.Metadata)
                });
            }

            return blobs;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to list blobs");
            throw new ApplicationException($"List Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Delete a blob
    /// Interview Tip: Explain soft delete feature (can recover deleted blobs within retention period)
    /// </summary>
    public async Task<bool> DeleteBlobAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            _logger.LogInformation("Deleting blob: {BlobName}", blobName);
            var response = await blobClient.DeleteIfExistsAsync();
            
            return response.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete blob");
            throw new ApplicationException($"Delete Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get blob metadata and properties without downloading
    /// Interview Tip: More efficient than downloading when you only need metadata
    /// </summary>
    public async Task<AzureOpenAISample.Models.BlobInfo?> GetBlobPropertiesAsync(string blobName)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            var properties = await blobClient.GetPropertiesAsync();
            
            return new AzureOpenAISample.Models.BlobInfo
            {
                Name = blobName,
                Uri = blobClient.Uri.ToString(),
                Size = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType,
                LastModified = properties.Value.LastModified,
                Metadata = new Dictionary<string, string>(properties.Value.Metadata)
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to get blob properties");
            throw new ApplicationException($"Get Properties Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate a Shared Access Signature (SAS) URL
    /// 
    /// Interview Talking Points:
    /// - SAS provides time-limited access without sharing account keys
    /// - Can restrict by IP, protocol (HTTPS only), permissions (read/write)
    /// - User Delegation SAS uses Azure AD credentials (more secure)
    /// - Service SAS uses account key
    /// - Stored Access Policy allows revoking multiple SAS tokens at once
    /// </summary>
    public async Task<string> GenerateSasUrlAsync(string blobName, TimeSpan validity)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Blob '{blobName}' not found");
            }

            // Check if we can generate SAS (requires account key)
            if (_useManagedIdentity)
            {
                _logger.LogWarning("SAS generation with Managed Identity requires User Delegation Key");
                // In production, implement User Delegation SAS for Managed Identity
                throw new NotSupportedException("SAS generation with Managed Identity requires additional implementation");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b", // b for blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Start 5 minutes ago to account for clock skew
                ExpiresOn = DateTimeOffset.UtcNow.Add(validity)
            };

            // Set permissions (Read in this case)
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
            
            _logger.LogInformation("Generated SAS URL for blob: {BlobName}, Valid for: {Validity}", blobName, validity);
            return sasUrl;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URL");
            throw new ApplicationException($"SAS Generation Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Copy a blob within the storage account
    /// Interview Tip: Copy is asynchronous on the service side for large blobs
    /// </summary>
    public async Task<bool> CopyBlobAsync(string sourceBlobName, string destinationBlobName)
    {
        try
        {
            var sourceBlobClient = _containerClient.GetBlobClient(sourceBlobName);
            var destBlobClient = _containerClient.GetBlobClient(destinationBlobName);

            _logger.LogInformation("Copying blob from {Source} to {Destination}", sourceBlobName, destinationBlobName);
            
            var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
            await copyOperation.WaitForCompletionAsync();
            
            return copyOperation.HasCompleted;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to copy blob");
            throw new ApplicationException($"Copy Error: {ex.Message}", ex);
        }
    }
}
