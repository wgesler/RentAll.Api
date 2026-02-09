using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;

namespace RentAll.Infrastructure.Services;

public class AzureBlobStorageService : IFileService
{
	private readonly BlobServiceClient _blobServiceClient;
	private readonly StorageSettings _storageSettings;
	private readonly ILogger<AzureBlobStorageService> _logger;

	public AzureBlobStorageService(
		BlobServiceClient blobServiceClient,
		StorageSettings storageSettings,
		ILogger<AzureBlobStorageService> logger)
	{
		_blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
		_storageSettings = storageSettings ?? throw new ArgumentNullException(nameof(storageSettings));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<string> SaveLogoAsync(Guid organizationId, int? officeId, string fileContent, string fileName, string contentType, EntityType entityType)
	{
		// Handle base64 encoded content
		byte[] fileBytes;
		if (fileContent.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
		{
			// Data URL format: data:image/png;base64,...
			var base64Data = fileContent.Split(',')[1];
			fileBytes = Convert.FromBase64String(base64Data);
		}
		else
		{
			// Assume it's already base64
			fileBytes = Convert.FromBase64String(fileContent);
		}

		using var stream = new MemoryStream(fileBytes);
		return await SaveLogoAsync(organizationId, officeId, stream, fileName, contentType, entityType);
	}

	public async Task<string> SaveLogoAsync(Guid organizationId, int? officeId, Stream fileStream, string fileName, string contentType, EntityType entityType)
	{
		// Validate file type
		var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
		var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
		if (!allowedExtensions.Contains(fileExtension))
			throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

		// Validate content type
		if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Content type must be an image type");

		try
		{
			// Use organization GUID and office ID as container name
			var containerName = (officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0").ToLowerInvariant();
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

			// Convert EntityType to lowercase string for filename
			var typeString = entityType.ToString().ToLowerInvariant();

			// Generate unique filename and store in logos folder
			var uniqueFileName = $"{typeString}-{Guid.NewGuid()}{fileExtension}";
			var blobName = $"logos/{uniqueFileName}";
			var blobClient = containerClient.GetBlobClient(blobName);

			// Upload file
			fileStream.Position = 0;
			await blobClient.UploadAsync(fileStream, new BlobUploadOptions
			{
				HttpHeaders = new BlobHttpHeaders
				{
					ContentType = contentType
				}
			});

			// Return URL or path
			if (!string.IsNullOrEmpty(_storageSettings.AzureBlobBaseUrl))
			{
				return $"{_storageSettings.AzureBlobBaseUrl.TrimEnd('/')}/{containerName}/{blobName}";
			}

			return blobClient.Uri.ToString();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving logo to Azure Blob Storage: {FileName}", fileName);
			throw;
		}
	}

	public async Task<bool> DeleteLogoAsync(Guid organizationId, int? officeId, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return false;

		try
		{
			// Extract blob name from URL or path
			var containerName = (officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0").ToLowerInvariant();
			var blobName = ExtractBlobName(filePath, containerName, "logos");
			if (string.IsNullOrEmpty(blobName))
				return false;

			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			var blobClient = containerClient.GetBlobClient(blobName);

			return await blobClient.DeleteIfExistsAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting logo from Azure Blob Storage: {FilePath}", filePath);
			return false;
		}
	}

	public async Task<FileDetails?> GetFileDetailsAsync(Guid organizationId, int? officeId, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		try
		{
			// Extract blob name from URL or path
			var containerName = (officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0").ToLowerInvariant();
			var blobName = ExtractBlobName(filePath, containerName, "logos");
			if (string.IsNullOrEmpty(blobName))
				return null;

			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			var blobClient = containerClient.GetBlobClient(blobName);

			if (!await blobClient.ExistsAsync())
				return null;

			// Download blob
			var downloadResult = await blobClient.DownloadContentAsync();
			var fileBytes = downloadResult.Value.Content.ToArray();
			var base64Content = Convert.ToBase64String(fileBytes);

			// Get content type from blob properties
			var properties = await blobClient.GetPropertiesAsync();
			var contentType = properties.Value.ContentType ?? "application/octet-stream";

			var fileName = Path.GetFileName(blobName);

			return new FileDetails
			{
				FileName = fileName,
				ContentType = contentType,
				File = base64Content,
				DataUrl = $"data:{contentType};base64,{base64Content}"
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error reading logo from Azure Blob Storage: {FilePath}", filePath);
			return null;
		}
	}

	public async Task<string> SaveDocumentAsync(Guid organizationId, int? officeId, string fileContent, string fileName, string contentType, DocumentType documentType)
	{
		// Handle base64 encoded content
		byte[] fileBytes;
		if (fileContent.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
		{
			// Data URL format: data:application/pdf;base64,...
			var base64Data = fileContent.Split(',')[1];
			fileBytes = Convert.FromBase64String(base64Data);
		}
		else
		{
			// Assume it's already base64
			fileBytes = Convert.FromBase64String(fileContent);
		}

		using var stream = new MemoryStream(fileBytes);
		return await SaveDocumentAsync(organizationId, officeId, stream, fileName, contentType, documentType);
	}

	public async Task<string> SaveDocumentAsync(Guid organizationId, int? officeId, Stream fileStream, string fileName, string contentType, DocumentType documentType)
	{
		// Validate file type
		var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpeg", ".jpg", ".png", ".txt" };
		var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
		if (!allowedExtensions.Contains(fileExtension))
			throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

		// Validate content type
		var allowedContentTypes = new[]
		{
			"application/pdf",
			"application/msword",
			"application/vnd.openxmlformats-officedocument.wordprocessingml.document",
			"image/jpeg",
			"image/png",
			"text/plain"
		};
		if (!allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
			throw new ArgumentException($"Invalid content type. Allowed types: {string.Join(", ", allowedContentTypes)}");

		try
		{
			// Use organization GUID and office ID as container name
			var containerName = (officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0").ToLowerInvariant();
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

			// Generate unique filename and store in documents/{documentType} folder
			var documentTypeFolder = documentType.ToString().ToLowerInvariant();
			var uniqueFileName = $"{documentTypeFolder}-{Guid.NewGuid()}{fileExtension}";
			var blobName = $"documents/{documentTypeFolder}/{uniqueFileName}";
			var blobClient = containerClient.GetBlobClient(blobName);

			// Upload file
			fileStream.Position = 0;
			await blobClient.UploadAsync(fileStream, new BlobUploadOptions
			{
				HttpHeaders = new BlobHttpHeaders
				{
					ContentType = contentType
				}
			});

			// Return URL or path
			if (!string.IsNullOrEmpty(_storageSettings.AzureBlobBaseUrl))
			{
				return $"{_storageSettings.AzureBlobBaseUrl.TrimEnd('/')}/{containerName}/{blobName}";
			}

			return blobClient.Uri.ToString();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving document to Azure Blob Storage: {FileName}", fileName);
			throw;
		}
	}

	public async Task<bool> DeleteDocumentAsync(Guid organizationId, int? officeId, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return false;

		try
		{
			// Extract blob name from URL or path
			var containerName = (officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0").ToLowerInvariant();
			var blobName = ExtractBlobNameFromDocumentPath(filePath, containerName);
			if (string.IsNullOrEmpty(blobName))
				return false;

			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			var blobClient = containerClient.GetBlobClient(blobName);

			return await blobClient.DeleteIfExistsAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting document from Azure Blob Storage: {FilePath}", filePath);
			return false;
		}
	}

	public async Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, int? officeId, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		try
		{
			// Extract blob name from URL or path
			var containerName = (officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0").ToLowerInvariant();
			var blobName = ExtractBlobNameFromDocumentPath(filePath, containerName);
			if (string.IsNullOrEmpty(blobName))
				return null;

			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			var blobClient = containerClient.GetBlobClient(blobName);

			if (!await blobClient.ExistsAsync())
				return null;

			// Download blob
			var downloadResult = await blobClient.DownloadContentAsync();
			var fileBytes = downloadResult.Value.Content.ToArray();
			var base64Content = Convert.ToBase64String(fileBytes);

			// Get content type from blob properties
			var properties = await blobClient.GetPropertiesAsync();
			var contentType = properties.Value.ContentType ?? "application/octet-stream";

			var fileName = Path.GetFileName(blobName);

			return new FileDetails
			{
				FileName = fileName,
				ContentType = contentType,
				File = base64Content,
				DataUrl = $"data:{contentType};base64,{base64Content}"
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error reading document from Azure Blob Storage: {FilePath}", filePath);
			return null;
		}
	}

	private string? ExtractBlobName(string filePath, string containerName, string folderName)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		var expectedPrefix = $"{folderName}/";

		// Handle full URL (e.g., https://account.blob.core.windows.net/{containerName}/logos/filename)
		if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
		{
			var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			
			// Try exact container name match first
			if (pathParts.Length >= 3 && pathParts[0].Equals(containerName, StringComparison.OrdinalIgnoreCase) 
				&& pathParts[1].Equals(folderName, StringComparison.OrdinalIgnoreCase))
			{
				return string.Join("/", pathParts.Skip(1)); // Return "logos/filename"
			}
			
			// Fallback: try to find the folder name in the path (in case container name format changed)
			// Look for the folder name (e.g., "logos") in the path parts
			for (int i = 0; i < pathParts.Length - 1; i++)
			{
				if (pathParts[i].Equals(folderName, StringComparison.OrdinalIgnoreCase))
				{
					// Found the folder name, return everything from this point forward
					return string.Join("/", pathParts.Skip(i));
				}
			}
		}

		// Handle relative path (e.g., /{containerName}/logos/filename)
		var normalizedPath = filePath.TrimStart('/').TrimEnd('/');
		if (normalizedPath.StartsWith($"{containerName}/{folderName}/", StringComparison.OrdinalIgnoreCase))
		{
			return normalizedPath.Substring(containerName.Length + 1); // Skip container name and leading slash
		}

		// Handle path without container name (e.g., logos/filename) - assume it's for this container
		if (normalizedPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return normalizedPath;
		}

		return null;
	}

	private string? ExtractBlobNameFromDocumentPath(string filePath, string containerName)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		// Handle full URL (e.g., https://account.blob.core.windows.net/{containerName}/documents/type/filename)
		if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
		{
			var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			// Path format: {containerName}/documents/{documentType}/{filename}
			if (pathParts.Length >= 4 && pathParts[0].Equals(containerName, StringComparison.OrdinalIgnoreCase)
				&& pathParts[1].Equals("documents", StringComparison.OrdinalIgnoreCase))
			{
				return string.Join("/", pathParts.Skip(1)); // Return "documents/{documentType}/{filename}"
			}
		}

		// Handle relative path (e.g., /{containerName}/documents/type/filename)
		var normalizedPath = filePath.TrimStart('/');
		if (normalizedPath.StartsWith($"{containerName}/documents/", StringComparison.OrdinalIgnoreCase))
		{
			return normalizedPath.Substring(containerName.Length + 1); // Skip container name and leading slash
		}

		// Handle path without container name (e.g., documents/type/filename) - assume it's for this container
		if (normalizedPath.StartsWith("documents/", StringComparison.OrdinalIgnoreCase))
		{
			return normalizedPath;
		}

		return null;
	}
}
