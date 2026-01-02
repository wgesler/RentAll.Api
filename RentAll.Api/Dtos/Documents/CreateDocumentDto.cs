using RentAll.Domain.Models;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Documents;

public class CreateDocumentDto
{
	public int? OfficeId { get; set; }
	public DocumentType DocumentType { get; set; }
	public FileDetails? FileDetails { get; set; }
	public bool IsDeleted { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (FileDetails == null || string.IsNullOrWhiteSpace(FileDetails.File))
			return (false, "File is required");

		if (string.IsNullOrWhiteSpace(FileDetails.FileName))
			return (false, "File name is required");

		if (string.IsNullOrWhiteSpace(FileDetails.ContentType))
			return (false, "Content type is required");

		// Validate content type is PDF
		if (!FileDetails.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
			return (false, "Only PDF files are allowed");

		return (true, null);
	}

	public Document ToModel(Guid organizationId, string documentPath, Guid currentUser)
	{
		var fileExtension = Path.GetExtension(FileDetails!.FileName);
		if (string.IsNullOrWhiteSpace(fileExtension))
			fileExtension = ".pdf"; // Default to .pdf if no extension
		
		return new Document
		{
			DocumentId = Guid.NewGuid(),
			OrganizationId = organizationId,
			OfficeId = OfficeId,
			DocumentType = DocumentType,
			FileName = FileDetails.FileName,
			FileExtension = fileExtension,
			ContentType = FileDetails.ContentType,
			DocumentPath = documentPath,
			IsDeleted = IsDeleted,
			CreatedBy = currentUser
		};
	}
}

