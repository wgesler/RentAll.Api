using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Documents;

public class UpdateDocumentDto
{
    public Guid DocumentId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public int DocumentTypeId { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsDeleted { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (DocumentId == Guid.Empty)
            return (false, "Document ID is required");

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

    public Document ToModel(Guid currentUser)
    {
        return new Document
        {
            DocumentId = DocumentId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            DocumentType = (DocumentType)DocumentTypeId,
            FileName = Path.GetFileNameWithoutExtension(FileDetails!.FileName),
            FileExtension = Path.GetExtension(FileDetails!.FileName),
            ContentType = FileDetails!.ContentType,
            DocumentPath = string.Empty,
            IsDeleted = IsDeleted,
            ModifiedBy = currentUser
        };
    }
}


