using RentAll.Domain.Models;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Documents;

public class CreateDocumentDto
{
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public int DocumentTypeId { get; set; }
	public FileDetails? FileDetails { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
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
			DocumentType = (DocumentType)DocumentTypeId,
			FileName = Path.GetFileNameWithoutExtension(FileDetails!.FileName),
			FileExtension = Path.GetExtension(FileDetails!.FileName),
			ContentType = FileDetails!.ContentType,
			DocumentPath = string.Empty,
			IsDeleted = false,
			CreatedBy = currentUser
		};
	}
}

