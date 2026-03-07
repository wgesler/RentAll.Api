using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;

namespace RentAll.Infrastructure.Services;

public class FileAttachmentHelper : IFileAttachmentHelper
{
    private readonly IFileService _fileService;

    public FileAttachmentHelper(IFileService fileService)
    {
        _fileService = fileService;
    }

    #region Image Helper Methods
    public async Task<string?> SaveImageIfPresentAsync(Guid organizationId, string? officeName, FileDetails? fileDetails, ImageType imageType)
    {
        if (fileDetails == null || string.IsNullOrWhiteSpace(fileDetails.File))
            return null;
        var name = string.IsNullOrWhiteSpace(fileDetails.FileName) ? "image" : fileDetails.FileName;
        var type = string.IsNullOrWhiteSpace(fileDetails.ContentType) ? "image/png" : fileDetails.ContentType;
        return await _fileService.SaveImageAsync(organizationId, officeName, fileDetails.File, name, type, imageType).ConfigureAwait(false);
    }

    public async Task<string?> ResolveImagePathForUpdateAsync(Guid organizationId, string? officeName, FileDetails? fileDetails, ImageType imageType, string? existingPath, string? dtoPath)
    {
        // (1) New file content: delete existing (by path), save new, return new path
        if (fileDetails != null && !string.IsNullOrWhiteSpace(fileDetails.File))
        {
            if (!string.IsNullOrWhiteSpace(existingPath))
                await _fileService.DeleteImageAsync(organizationId, officeName, existingPath, imageType).ConfigureAwait(false);
            var name = string.IsNullOrWhiteSpace(fileDetails.FileName) ? "image" : fileDetails.FileName;
            var type = string.IsNullOrWhiteSpace(fileDetails.ContentType) ? "image/png" : fileDetails.ContentType;
            return await _fileService.SaveImageAsync(organizationId, officeName, fileDetails.File, name, type, imageType).ConfigureAwait(false);
        }
        // (2) No content and path null = explicit clear: delete existing, return null
        if (dtoPath == null)
        {
            if (!string.IsNullOrWhiteSpace(existingPath))
                await _fileService.DeleteImageAsync(organizationId, officeName, existingPath, imageType).ConfigureAwait(false);
            return null;
        }
        // (3) No new file, path not cleared: no change
        return existingPath;
    }

    public async Task<FileDetails?> GetImageDetailsForResponseAsync(Guid organizationId, string? officeName, string? path, ImageType imageType)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        return await _fileService.GetImageDetailsAsync(organizationId, officeName, path, imageType).ConfigureAwait(false);
    }
    #endregion

    #region Document Helper Methods
    public async Task<string?> SaveDocumentIfPresentAsync(Guid organizationId, string? officeName, FileDetails? fileDetails, DocumentType documentType)
    {
        if (fileDetails == null || string.IsNullOrWhiteSpace(fileDetails.File))
            return null;
        var name = string.IsNullOrWhiteSpace(fileDetails.FileName) ? "document" : fileDetails.FileName;
        var type = string.IsNullOrWhiteSpace(fileDetails.ContentType) ? "application/octet-stream" : fileDetails.ContentType;
        return await _fileService.SaveDocumentAsync(organizationId, officeName, fileDetails.File, name, type, documentType).ConfigureAwait(false);
    }

    public async Task<string?> ResolveDocumentPathForUpdateAsync(Guid organizationId, string? officeName, FileDetails? fileDetails, DocumentType documentType, string? existingPath, string? dtoPath)
    {
        // (1) New file content: delete existing, save new, return new path
        if (fileDetails != null && !string.IsNullOrWhiteSpace(fileDetails.File))
        {
            if (!string.IsNullOrWhiteSpace(existingPath))
                await _fileService.DeleteDocumentAsync(organizationId, officeName, existingPath).ConfigureAwait(false);
            var name = string.IsNullOrWhiteSpace(fileDetails.FileName) ? "document" : fileDetails.FileName;
            var type = string.IsNullOrWhiteSpace(fileDetails.ContentType) ? "application/octet-stream" : fileDetails.ContentType;
            return await _fileService.SaveDocumentAsync(organizationId, officeName, fileDetails.File, name, type, documentType).ConfigureAwait(false);
        }
        // (2) Explicit clear
        if (dtoPath == null)
        {
            if (!string.IsNullOrWhiteSpace(existingPath))
                await _fileService.DeleteDocumentAsync(organizationId, officeName, existingPath).ConfigureAwait(false);
            return null;
        }
        // (3) No change
        return existingPath;
    }

    public async Task<FileDetails?> GetDocumentDetailsForResponseAsync(Guid organizationId, string? officeName, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        return await _fileService.GetDocumentDetailsAsync(organizationId, officeName, path).ConfigureAwait(false);
    }
    #endregion
}
