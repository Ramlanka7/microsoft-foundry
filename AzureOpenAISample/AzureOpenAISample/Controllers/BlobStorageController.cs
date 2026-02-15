using AzureOpenAISample.Models;
using AzureOpenAISample.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureOpenAISample.Controllers;

/// <summary>
/// Controller demonstrating Azure Blob Storage operations
/// 
/// Interview Preparation Topics:
/// 1. Blob types: Block, Page, Append blobs
/// 2. Storage tiers: Hot, Cool, Archive (cost optimization)
/// 3. SAS tokens for secure temporary access
/// 4. Lifecycle management policies
/// 5. Blob versioning and soft delete
/// 6. CDN integration for global distribution
/// 7. Performance optimization (parallel uploads, resumable uploads)
/// </summary>
[ApiController]
[Route("api/blobstorage")]
public class BlobStorageController : ControllerBase
{
    private readonly IAzureBlobStorageService _blobService;
    private readonly ILogger<BlobStorageController> _logger;

    public BlobStorageController(
        IAzureBlobStorageService blobService,
        ILogger<BlobStorageController> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    /// <summary>
    /// POST: api/BlobStorage/upload
    /// Upload a file to blob storage
    /// 
    /// Interview Tip: Explain multipart/form-data for file uploads
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string? blobName = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        try
        {
            // Use provided blob name or generate from file name
            var finalBlobName = !string.IsNullOrEmpty(blobName) ? blobName : file.FileName;
            
            var metadata = new Dictionary<string, string>
            {
                ["OriginalFileName"] = file.FileName,
                ["UploadedAt"] = DateTime.UtcNow.ToString("O"),
                ["FileSize"] = file.Length.ToString()
            };

            using var stream = file.OpenReadStream();
            var blobInfo = await _blobService.UploadBlobAsync(
                finalBlobName, 
                stream, 
                file.ContentType, 
                metadata);

            return Ok(new
            {
                success = true,
                message = "File uploaded successfully",
                blob = blobInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/BlobStorage/upload-text
    /// Upload text content as a blob
    /// 
    /// Example Request:
    /// {
    ///   "blobName": "sample.txt",
    ///   "content": "Hello from Azure Blob Storage!",
    ///   "contentType": "text/plain"
    /// }
    /// </summary>
    [HttpPost("upload-text")]
    public async Task<IActionResult> UploadText([FromBody] TextUploadRequest request)
    {
        if (string.IsNullOrEmpty(request.BlobName) || string.IsNullOrEmpty(request.Content))
        {
            return BadRequest("BlobName and Content are required");
        }

        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(request.Content);
            using var stream = new MemoryStream(bytes);

            var metadata = new Dictionary<string, string>
            {
                ["ContentLength"] = bytes.Length.ToString(),
                ["UploadedAt"] = DateTime.UtcNow.ToString("O")
            };

            var blobInfo = await _blobService.UploadBlobAsync(
                request.BlobName, 
                stream, 
                request.ContentType ?? "text/plain", 
                metadata);

            return Ok(new
            {
                success = true,
                message = "Text content uploaded successfully",
                blob = blobInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text upload failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET: api/BlobStorage/download/{blobName}
    /// Download a blob
    /// Interview Tip: Explain Content-Disposition header for browser download behavior
    /// </summary>
    [HttpGet("download/{*blobName}")]
    public async Task<IActionResult> DownloadBlob(string blobName)
    {
        try
        {
            // Get properties first to set correct content type
            var properties = await _blobService.GetBlobPropertiesAsync(blobName);
            if (properties == null)
            {
                return NotFound(new { message = $"Blob '{blobName}' not found" });
            }

            var stream = await _blobService.DownloadBlobAsync(blobName);
            
            return File(stream, properties.ContentType, blobName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET: api/BlobStorage/list
    /// List all blobs or blobs with a specific prefix
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListBlobs([FromQuery] string? prefix = null)
    {
        try
        {
            var blobs = await _blobService.ListBlobsAsync(prefix);
            return Ok(new
            {
                count = blobs.Count,
                prefix = prefix ?? "none",
                blobs = blobs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List blobs failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET: api/BlobStorage/properties/{blobName}
    /// Get blob properties without downloading
    /// Interview Tip: More efficient when you only need metadata
    /// </summary>
    [HttpGet("properties/{*blobName}")]
    public async Task<IActionResult> GetBlobProperties(string blobName)
    {
        try
        {
            var properties = await _blobService.GetBlobPropertiesAsync(blobName);
            if (properties == null)
            {
                return NotFound(new { message = $"Blob '{blobName}' not found" });
            }
            return Ok(properties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get properties failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE: api/BlobStorage/{blobName}
    /// Delete a blob
    /// </summary>
    [HttpDelete("{*blobName}")]
    public async Task<IActionResult> DeleteBlob(string blobName)
    {
        try
        {
            var success = await _blobService.DeleteBlobAsync(blobName);
            if (success)
            {
                return Ok(new { success = true, message = $"Blob '{blobName}' deleted successfully" });
            }
            return NotFound(new { message = $"Blob '{blobName}' not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/BlobStorage/sas/{blobName}
    /// Generate a SAS URL for temporary access
    /// 
    /// Interview Talking Points:
    /// - SAS provides secure, time-limited access
    /// - No need to share storage account keys
    /// - Can restrict by IP, permissions, protocol
    /// - User Delegation SAS uses Azure AD (more secure)
    /// </summary>
    [HttpPost("sas/{*blobName}")]
    public async Task<IActionResult> GenerateSasUrl(string blobName, [FromQuery] int validityMinutes = 60)
    {
        try
        {
            var validity = TimeSpan.FromMinutes(validityMinutes);
            var sasUrl = await _blobService.GenerateSasUrlAsync(blobName, validity);
            
            return Ok(new
            {
                blobName = blobName,
                sasUrl = sasUrl,
                validFor = $"{validityMinutes} minutes",
                expiresAt = DateTime.UtcNow.AddMinutes(validityMinutes)
            });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAS generation failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST: api/BlobStorage/copy
    /// Copy a blob within the container
    /// </summary>
    [HttpPost("copy")]
    public async Task<IActionResult> CopyBlob([FromBody] CopyBlobRequest request)
    {
        if (string.IsNullOrEmpty(request.SourceBlobName) || string.IsNullOrEmpty(request.DestinationBlobName))
        {
            return BadRequest("SourceBlobName and DestinationBlobName are required");
        }

        try
        {
            var success = await _blobService.CopyBlobAsync(request.SourceBlobName, request.DestinationBlobName);
            return Ok(new
            {
                success = success,
                message = $"Blob copied from '{request.SourceBlobName}' to '{request.DestinationBlobName}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class TextUploadRequest
{
    public string BlobName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}

public class CopyBlobRequest
{
    public string SourceBlobName { get; set; } = string.Empty;
    public string DestinationBlobName { get; set; } = string.Empty;
}
