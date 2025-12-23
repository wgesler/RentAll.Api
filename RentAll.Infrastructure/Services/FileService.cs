using Microsoft.Extensions.Logging;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;

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
}

