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

	public async Task<string> SaveLogoAsync(string fileContent, string fileName, string contentType, EntityType entityType)
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
		return await SaveLogoAsync(stream, fileName, contentType, entityType);
	}

	public async Task<string> SaveLogoAsync(Stream fileStream, string fileName, string contentType, EntityType entityType)
	{
		// Validate file type
		var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
		var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
		if (!allowedExtensions.Contains(fileExtension))
			throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

		// Validate content type
		if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Content type must be an image type");

		// Create directory if it doesn't exist
		var uploadsPath = Path.Combine(_wwwRootPath, "images", "logos");
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
		return $"/images/logos/{uniqueFileName}";
	}

	public Task<bool> DeleteLogoAsync(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return Task.FromResult(false);

		// Security: Ensure path is within wwwroot/images/logos
		if (!filePath.StartsWith("/images/logos/", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Attempted to delete file outside allowed directory: {FilePath}", filePath);
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

	public async Task<FileDetails?> GetFileDetailsAsync(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		// Security: Ensure path is within wwwroot/images/logos
		if (!filePath.StartsWith("/images/logos/", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Attempted to read file outside allowed directory: {FilePath}", filePath);
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

	public async Task<string> SaveDocumentAsync(string fileContent, string fileName, string contentType, DocumentType documentType)
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
		return await SaveDocumentAsync(stream, fileName, contentType, documentType);
	}

	public async Task<string> SaveDocumentAsync(Stream fileStream, string fileName, string contentType, DocumentType documentType)
	{
		// Validate file type
		var allowedExtensions = new[] { ".pdf" };
		var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
		if (!allowedExtensions.Contains(fileExtension))
			throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

		// Validate content type
		if (!contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Content type must be application/pdf");

		// Create directory if it doesn't exist
		var uploadsPath = Path.Combine(_wwwRootPath, "documents", documentType.ToString().ToLowerInvariant());
		Directory.CreateDirectory(uploadsPath);

		// Generate unique filename
		var uniqueFileName = $"{documentType.ToString().ToLowerInvariant()}-{Guid.NewGuid()}{fileExtension}";
		var fullPath = Path.Combine(uploadsPath, uniqueFileName);

		// Save file
		using (var fileStreamOut = new FileStream(fullPath, FileMode.Create))
		{
			await fileStream.CopyToAsync(fileStreamOut);
		}

		// Return relative path
		return $"/documents/{documentType.ToString().ToLowerInvariant()}/{uniqueFileName}";
	}

	public Task<bool> DeleteDocumentAsync(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return Task.FromResult(false);

		// Security: Ensure path is within wwwroot/documents
		if (!filePath.StartsWith("/documents/", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Attempted to delete file outside allowed directory: {FilePath}", filePath);
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

	public async Task<FileDetails?> GetDocumentDetailsAsync(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		// Security: Ensure path is within wwwroot/documents
		if (!filePath.StartsWith("/documents/", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Attempted to read file outside allowed directory: {FilePath}", filePath);
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

