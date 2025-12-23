using RentAll.Domain.Enums;

namespace RentAll.Domain.Interfaces.Services;

public interface IFileService
{
	Task<string> SaveLogoAsync(string fileContent, string fileName, string contentType, EntityType entityType);
	Task<string> SaveLogoAsync(Stream fileStream, string fileName, string contentType, EntityType entityType);
	Task<bool> DeleteLogoAsync(string filePath);
}

