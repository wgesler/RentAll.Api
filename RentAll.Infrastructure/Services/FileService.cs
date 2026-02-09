using Microsoft.Extensions.Logging;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;

namespace RentAll.Infrastructure.Services;

public class FileService : IFileService
{
	private readonly string _wwwRootPath;
	private readonly ILogger<FileService> _logger;

	public FileService(string wwwRootPath, ILogger<FileService> logger)
	{
		_wwwRootPath = wwwRootPath ?? throw new ArgumentNullException(nameof(wwwRootPath));
		_logger = logger;
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

		// Create directory if it doesn't exist - organized by organization GUID and Office ID
		var containerName = officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0";
		var uploadsPath = Path.Combine(_wwwRootPath, containerName, "logos");
		Directory.CreateDirectory(uploadsPath);

		// Convert EntityType to lowercase string for filename
		var typeString = entityType.ToString().ToLowerInvariant();

		// Generate unique filename
		var uniqueFileName = $"{typeString}-{Guid.NewGuid()}{fileExtension}";
		var fullPath = Path.Combine(uploadsPath, uniqueFileName);

		// Save file
		using (var fileStreamOut = new FileStream(fullPath, FileMode.Create))
		{
			await fileStream.CopyToAsync(fileStreamOut);
		}

		// Return relative path
		return $"/{containerName}/logos/{uniqueFileName}";
	}

	public Task<bool> DeleteLogoAsync(Guid organizationId, int? officeId, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return Task.FromResult(false);

		// Security: Ensure path is within the organization/office's logos directory
		var containerName = officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0";
		var expectedPathPrefix = $"/{containerName}/logos/";
		if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Attempted to delete file outside allowed directory: {FilePath} for organization {OrganizationId}, office {OfficeId}", filePath, organizationId, officeId);
			return Task.FromResult(false);
		}

		try
		{
			var fullPath = Path.Combine(
				_wwwRootPath,
				filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

			if (File.Exists(fullPath))
			{
				File.Delete(fullPath);
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting logo file: {FilePath}", filePath);
			return Task.FromResult(false);
		}
	}

	public async Task<FileDetails?> GetFileDetailsAsync(Guid organizationId, int? officeId, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		// Security: Ensure path is within the organization/office's logos directory
		var containerName = officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0";
		var expectedPathPrefix = $"/{containerName}/logos/";
		if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Attempted to read file outside allowed directory: {FilePath} for organization {OrganizationId}, office {OfficeId}", filePath, organizationId, officeId);
			return null;
		}

		try
		{
			var fullPath = Path.Combine(
				_wwwRootPath,
				filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

			if (!File.Exists(fullPath))
				return null;

			var fileBytes = await File.ReadAllBytesAsync(fullPath);
			var base64Content = Convert.ToBase64String(fileBytes);
			var fileName = Path.GetFileName(filePath);
			var extension = Path.GetExtension(filePath).ToLowerInvariant();

			// Determine content type from extension
			var contentType = extension switch
			{
				".png" => "image/png",
				".jpg" or ".jpeg" => "image/jpeg",
				".gif" => "image/gif",
				".svg" => "image/svg+xml",
				_ => "application/octet-stream"
			};

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
			_logger.LogError(ex, "Error reading logo file: {FilePath}", filePath);
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

		// Create directory if it doesn't exist - organized by organization GUID and office ID
		var containerName = officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0";
		var documentTypeFolder = documentType.ToString().ToLowerInvariant();
		var uploadsPath = Path.Combine(_wwwRootPath, containerName, "documents", documentTypeFolder);
		Directory.CreateDirectory(uploadsPath);

		// Generate unique filename
		var uniqueFileName = $"{documentTypeFolder}-{Guid.NewGuid()}{fileExtension}";
		var fullPath = Path.Combine(uploadsPath, uniqueFileName);

		// Save file
		using (var fileStreamOut = new FileStream(fullPath, FileMode.Create))
		{
			await fileStream.CopyToAsync(fileStreamOut);
		}

		// Return relative path
		return $"/{containerName}/documents/{documentTypeFolder}/{uniqueFileName}";
	}

	public Task<bool> DeleteDocumentAsync(Guid organizationId, int? officeId, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return Task.FromResult(false);

		// Security: Ensure path is within the organization/office's documents directory
		var containerName = officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0";
		var expectedPathPrefix = $"/{containerName}/documents/";
		if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Attempted to delete file outside allowed directory: {FilePath} for organization {OrganizationId}, office {OfficeId}", filePath, organizationId, officeId);
			return Task.FromResult(false);
		}

		try
		{
			var fullPath = Path.Combine(
				_wwwRootPath,
				filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

			if (File.Exists(fullPath))
			{
				File.Delete(fullPath);
				return Task.FromResult(true);
			}

			return Task.FromResult(false);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting document file: {FilePath}", filePath);
			return Task.FromResult(false);
		}
	}

	public async Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, int? officeId, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		// Security: Ensure path is within the organization/office's documents directory
		var containerName = officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0";
		var expectedPathPrefix = $"/{containerName}/documents/";
		if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Attempted to read file outside allowed directory: {FilePath} for organization {OrganizationId}, office {OfficeId}", filePath, organizationId, officeId);
			return null;
		}

		try
		{
			var fullPath = Path.Combine(
				_wwwRootPath,
				filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

			if (!File.Exists(fullPath))
				return null;

			var fileBytes = await File.ReadAllBytesAsync(fullPath);
			var base64Content = Convert.ToBase64String(fileBytes);
			var fileName = Path.GetFileName(filePath);
			var extension = Path.GetExtension(filePath).ToLowerInvariant();

			// Determine content type from extension
			var contentType = extension switch
			{
				".pdf" => "application/pdf",
				".doc" => "application/msword",
				".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".txt" => "text/plain",
				_ => "application/octet-stream"
			};

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
			_logger.LogError(ex, "Error reading document file: {FilePath}", filePath);
			return null;
		}
	}
}

