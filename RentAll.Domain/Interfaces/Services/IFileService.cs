using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IFileService
{
	Task<string> SaveLogoAsync(string fileContent, string fileName, string contentType, EntityType entityType);
	Task<string> SaveLogoAsync(Stream fileStream, string fileName, string contentType, EntityType entityType);
	Task<bool> DeleteLogoAsync(string filePath);
	Task<FileDetails?> GetFileDetailsAsync(string filePath);
}

