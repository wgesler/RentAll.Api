using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

/// <summary>
/// Shared logic for saving file content to storage (when present) and populating FileDetails for API responses.
/// Use this so all controllers (User, Document, Photo, Receipts, Logos, Contact, etc.) follow the same pattern.
/// </summary>
public interface IFileAttachmentHelper
{
    /// <summary>
    /// Saves image to storage when fileDetails has content. Returns the stored path, or null if fileDetails is null or content is empty.
    /// Use for create flows.
    /// </summary>
    Task<string?> SaveImageIfPresentAsync(Guid organizationId, string? officeName, FileDetails? fileDetails, ImageType imageType);

    /// <summary>
    /// Update flow: (1) If fileDetails has content → delete existing (by existingPath), save new, return new path.
    /// (2) Else if dtoPath is null (explicit clear) → delete existing, return null.
    /// (3) Else → return existingPath (no change).
    /// </summary>
    Task<string?> ResolveImagePathForUpdateAsync(Guid organizationId, string? officeName, FileDetails? fileDetails, ImageType imageType, string? existingPath, string? dtoPath);

    /// <summary>
    /// Saves document to storage when fileDetails has content. Returns the stored path, or null if fileDetails is null or content is empty.
    /// </summary>
    Task<string?> SaveDocumentIfPresentAsync(Guid organizationId, string? officeName, FileDetails? fileDetails, DocumentType documentType);

    /// <summary>
    /// Update flow for documents: same three-branch logic as ResolveImagePathForUpdateAsync.
    /// </summary>
    Task<string?> ResolveDocumentPathForUpdateAsync(Guid organizationId, string? officeName, FileDetails? fileDetails, DocumentType documentType, string? existingPath, string? dtoPath);

    /// <summary>
    /// Gets file details for an image path to attach to a response. Returns null if path is null/empty.
    /// </summary>
    Task<FileDetails?> GetImageDetailsForResponseAsync(Guid organizationId, string? officeName, string? path, ImageType imageType);

    /// <summary>
    /// Gets file details for a document path to attach to a response. Returns null if path is null/empty.
    /// </summary>
    Task<FileDetails?> GetDocumentDetailsForResponseAsync(Guid organizationId, string? officeName, string? path);
}
