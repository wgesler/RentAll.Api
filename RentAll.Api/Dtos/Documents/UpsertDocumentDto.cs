using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Documents;

public class UpsertDocumentDto
{
    public string FileName { get; set; } = string.Empty;
    public int? OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public int DocumentTypeId { get; set; }
    public FileDetails? FileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(FileName))
            return (false, "File name is required");

        if (FileDetails == null || string.IsNullOrWhiteSpace(FileDetails.File))
            return (false, "File is required");

        if (string.IsNullOrWhiteSpace(FileDetails.FileName))
            return (false, "File name is required");

        if (string.IsNullOrWhiteSpace(FileDetails.ContentType))
            return (false, "Content type is required");

        // Validate enum value
        if (!Enum.IsDefined(typeof(DocumentType), DocumentTypeId))
            return (false, $"Invalid Document value: {DocumentTypeId}");

        return (true, null);
    }

    public Document ToModel(Guid organizationId, Guid currentUser)
    {
        return new Document
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            DocumentType = (DocumentType)DocumentTypeId,
            FileName = FileName,
            FileExtension = Path.GetExtension(FileDetails!.FileName),
            ContentType = FileDetails!.ContentType,
            DocumentPath = string.Empty,
            IsDeleted = false,
            CreatedBy = currentUser
        };
    }

    public Document ToModelForUpdate(Document existing, Guid currentUser)
    {
        return new Document
        {
            DocumentId = existing.DocumentId,
            OrganizationId = existing.OrganizationId,
            OfficeId = OfficeId ?? existing.OfficeId,
            PropertyId = PropertyId ?? existing.PropertyId,
            ReservationId = ReservationId ?? existing.ReservationId,
            DocumentType = (DocumentType)DocumentTypeId,
            FileName = FileName,
            FileExtension = Path.GetExtension(FileDetails!.FileName),
            ContentType = FileDetails!.ContentType,
            DocumentPath = string.Empty, // Will be set when saving file
            IsDeleted = existing.IsDeleted,
            CreatedOn = existing.CreatedOn,
            CreatedBy = existing.CreatedBy,
            ModifiedBy = currentUser
        };
    }
}
