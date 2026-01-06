using RentAll.Domain.Models;
using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Documents;

public class DocumentResponseDto
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
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }

	public DocumentResponseDto(Document document)
	{
		DocumentId = document.DocumentId;
		OrganizationId = document.OrganizationId;
		OfficeId = document.OfficeId;
		DocumentType = document.DocumentType;
		FileName = document.FileName;
		FileExtension = document.FileExtension;
		ContentType = document.ContentType;
		DocumentPath = document.DocumentPath;
		IsDeleted = document.IsDeleted;
		CreatedOn = document.CreatedOn;
		CreatedBy = document.CreatedBy;
		ModifiedOn = document.ModifiedOn;
		ModifiedBy = document.ModifiedBy;
	}
}


