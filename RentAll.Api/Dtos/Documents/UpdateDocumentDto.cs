using RentAll.Domain.Models;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Documents;

public class UpdateDocumentDto
{
	public Guid DocumentId { get; set; }
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public DocumentType DocumentType { get; set; }
	public string FileName { get; set; } = string.Empty;
	public string FileExtension { get; set; } = string.Empty;
	public string ContentType { get; set; } = string.Empty;
	public string DocumentPath { get; set; } = string.Empty;
	public FileDetails? FileDetails { get; set; }
	public bool IsDeleted { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
	{
		if (id == Guid.Empty)
			return (false, "Document ID is required");

		if (DocumentId != id)
			return (false, "Document ID mismatch");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (string.IsNullOrWhiteSpace(FileName))
			return (false, "File name is required");

		if (string.IsNullOrWhiteSpace(DocumentPath))
			return (false, "Document path is required");

		// If FileDetails is provided, validate it
		if (FileDetails != null && !string.IsNullOrWhiteSpace(FileDetails.File))
		{
			if (!FileDetails.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
				return (false, "Only PDF files are allowed");
		}

		return (true, null);
	}

	public Document ToModel(Guid currentUser)
	{
		return new Document
		{
			DocumentId = DocumentId,
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			DocumentType = DocumentType,
			FileName = FileName,
			FileExtension = FileExtension,
			ContentType = ContentType,
			DocumentPath = DocumentPath,
			IsDeleted = IsDeleted,
			ModifiedBy = currentUser
		};
	}
}

